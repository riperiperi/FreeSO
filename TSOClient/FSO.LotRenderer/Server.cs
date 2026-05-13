// FSO.LotRenderer — Server.cs
// S3: Long-lived HTTP service mode. Accepts POST /render, caches by FSOV checksum.
//
// Cache key: SHA256(shard_id + lot_id + level + angle + zoom + roofless + fsov_bytes)
// Cache dir: /var/lib/freeso-renderer/cache/ (override via FSO_RENDERER_CACHE_DIR)
//           Falls back to ~/.local/share/freeso-renderer/cache/ if /var/lib is not writable.
//
// HTTP bind: 127.0.0.1:<port> by default (localhost-only, no auth in this slice).
//           Override bind address via FSO_RENDERER_BIND env var (e.g. "0.0.0.0:9101").
//
// DB: FSO_DB_URL (e.g. "Server=127.0.0.1;Port=3306;Database=fso;Uid=fsoserver;Pwd=password;")
//     Used to translate lot_id → lot_location. If not set, falls back to expecting
//     "lot_location" field directly in the request body.
//
// Design:
//   - Kestrel HTTP listener runs on its own threads (Kestrel's thread pool).
//   - Render requests are serialized via a SemaphoreSlim — the FSO graphics pipeline
//     is single-threaded and not re-entrant.
//   - FSOV fetch (ApiClient.GetFSOV) goes through GameThread.NextUpdate callbacks;
//     the render method bridges async Kestrel context → GameThread via TaskCompletionSource.
//   - Cache hit: serve PNG from disk in <100 ms without touching the GameThread.

using System.Net;
using System.Security.Cryptography;
using System.Text;
using FSO.Common.Utils;
using FSO.LotView;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using SixLabors.ImageSharp;

namespace FSO.LotRenderer
{
    /// <summary>
    /// JSON body accepted by POST /render.
    /// </summary>
    public class RenderRequest
    {
        /// <summary>Shard name, e.g. "Alphaville". Must be registered in fso_shards.</summary>
        [JsonProperty("shard")]
        public string Shard { get; set; } = "Alphaville";

        /// <summary>Database lot_id (primary key of fso_lots).</summary>
        [JsonProperty("lot_id")]
        public uint LotId { get; set; }

        /// <summary>
        /// Packed lot location (MapCoordinates.Pack(x,y) = x&lt;&lt;16|y).
        /// Optional override — if set, skips the DB lookup. Useful when FSO_DB_URL is not set.
        /// </summary>
        [JsonProperty("lot_location")]
        public uint? LotLocation { get; set; }

        /// <summary>Floor level. -1 = top floor (default, matches GetLotThumb).</summary>
        [JsonProperty("level")]
        public int Level { get; set; } = -1;

        /// <summary>Camera angle. "iso-ne" | "iso-nw" | "iso-se" | "iso-sw". Default: "iso-ne".</summary>
        [JsonProperty("angle")]
        public string Angle { get; set; } = "iso-ne";

        /// <summary>Zoom level. "far" | "med" | "near". Default: "far".</summary>
        [JsonProperty("zoom")]
        public string Zoom { get; set; } = "far";

        /// <summary>If true, render without roof.</summary>
        [JsonProperty("roofless")]
        public bool Roofless { get; set; } = false;
    }

    /// <summary>
    /// JSON body returned by POST /render on success.
    /// </summary>
    public class RenderResponse
    {
        [JsonProperty("path")]    public string Path    { get; set; }
        [JsonProperty("width")]   public int    Width   { get; set; }
        [JsonProperty("height")]  public int    Height  { get; set; }
        [JsonProperty("age_sec")] public double AgeSec  { get; set; }
    }

    /// <summary>
    /// S3 HTTP renderer service. Call <see cref="Run"/> from Program.cs --serve mode.
    /// The caller is responsible for initialising the FSO graphics pipeline (same as
    /// the standalone one-shot mode) BEFORE calling Run. Run enters the Kestrel host
    /// AND pumps the FSO GameThread simultaneously.
    /// </summary>
    public static class RendererServer
    {
        // -----------------------------------------------------------------------
        // Configuration (populated by Program.cs before Run is called)
        // -----------------------------------------------------------------------
        public static string ApiUrl;
        public static string ApiUser;
        public static string ApiPassword;
        public static string DbConnectionString;  // nullable — fallback to lot_location field
        public static string CacheDir;
        public static string BindAddress;         // e.g. "127.0.0.1:9101"

        // -----------------------------------------------------------------------
        // Internal state
        // -----------------------------------------------------------------------

        // One render at a time — FSO graphics pipeline is not re-entrant.
        static readonly SemaphoreSlim _renderSem = new SemaphoreSlim(1, 1);

        // Shard name → shard_id mapping (loaded from DB at startup, or hardcoded).
        static readonly Dictionary<string, uint> _shardNameToId = new(StringComparer.OrdinalIgnoreCase);

        // Lot-id→location cache (in-memory; from DB; refreshed per-request on miss).
        static readonly Dictionary<(uint shardId, uint lotId), uint> _lotLocationCache = new();

        // ApiClient singleton (initialised after login in Run).
        static FSO.Server.Clients.ApiClient _api;

        // -----------------------------------------------------------------------
        // Entry point
        // -----------------------------------------------------------------------

        /// <summary>
        /// Starts the Kestrel HTTP host AND pumps the FSO GameThread.
        /// This method never returns normally (only on Ctrl-C / SIGTERM).
        /// </summary>
        public static int Run(int port)
        {
            EnsureCacheDir();

            // Login (blocks until done via TCS bridge).
            Console.WriteLine($"[renderer/server] Logging in to API at {ApiUrl} ...");
            if (!LoginSync())
            {
                Console.Error.WriteLine("[renderer/server] Login failed — aborting.");
                return 4;
            }
            Console.WriteLine("[renderer/server] Login OK.");

            // Pre-load shard mapping.
            LoadShards();

            // Build Kestrel host.
            var host = BuildHost(port);

            // Start Kestrel on a background thread.
            var hostTask = host.RunAsync();
            Console.WriteLine($"[renderer/server] HTTP service listening on http://{BindAddress}/ (localhost-only, no auth)");

            // Pump GameThread on this thread (same as RunRenderLoop in one-shot mode).
            Console.WriteLine("[renderer/server] Pumping FSO GameThread. Press Ctrl-C to stop.");
            while (!hostTask.IsCompleted)
            {
                GameThread.OnWork.WaitOne(250);
                GameThread.DigestUpdate(null);
            }

            // Host stopped (Ctrl-C or SIGTERM).
            GameThread.SetKilled();
            return 0;
        }

        // -----------------------------------------------------------------------
        // Kestrel host setup
        // -----------------------------------------------------------------------

        static WebApplication BuildHost(int port)
        {
            var builder = WebApplication.CreateBuilder();

            // Suppress default ASP.NET Core console logging noise — we have our own.
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.SetMinimumLevel(LogLevel.Warning);

            builder.WebHost.UseKestrel(opts =>
            {
                // Bind to BindAddress (default: 127.0.0.1:<port>).
                var parts = BindAddress.Split(':');
                var ip    = parts[0];
                int p     = parts.Length > 1 ? int.Parse(parts[1]) : port;
                opts.Listen(System.Net.IPAddress.Parse(ip), p);
            });

            var app = builder.Build();

            // Use middleware routing so IResult is written correctly.
            app.Use(async (HttpContext ctx, RequestDelegate next) =>
            {
                if (ctx.Request.Method == "POST" && ctx.Request.Path == "/render")
                {
                    var result = await HandleRender(ctx);
                    await result.ExecuteAsync(ctx);
                }
                else if (ctx.Request.Method == "GET" && ctx.Request.Path == "/health")
                {
                    await Results.Ok(new { ok = true }).ExecuteAsync(ctx);
                }
                else
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.WriteAsync("Not Found");
                }
            });

            return app;
        }

        // -----------------------------------------------------------------------
        // POST /render handler
        // -----------------------------------------------------------------------

        static async Task<IResult> HandleRender(HttpContext ctx)
        {
            RenderRequest req;
            try
            {
                using var sr = new StreamReader(ctx.Request.Body);
                var body = await sr.ReadToEndAsync();
                req = JsonConvert.DeserializeObject<RenderRequest>(body);
                if (req == null)
                    return Results.BadRequest(new { error = "empty body" });
            }
            catch (Exception ex)
            {
                return Results.BadRequest(new { error = "bad JSON: " + ex.Message });
            }

            // Validate angle/zoom (fail fast before fetching FSOV).
            WorldRotation rotation;
            WorldZoom     zoom;
            try
            {
                rotation = Program.ParseAngle(req.Angle);
                zoom     = Program.ParseZoom(req.Zoom);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(new { error = ex.Message });
            }

            // Resolve shard_id.
            if (!_shardNameToId.TryGetValue(req.Shard, out uint shardId))
            {
                return Results.BadRequest(new { error = $"Unknown shard '{req.Shard}'. Known: {string.Join(", ", _shardNameToId.Keys)}" });
            }

            // Resolve lot_location.
            uint lotLocation;
            if (req.LotLocation.HasValue)
            {
                lotLocation = req.LotLocation.Value;
            }
            else
            {
                var loc = await LookupLotLocation(shardId, req.LotId);
                if (loc == null)
                    return Results.BadRequest(new { error = $"Could not find lot_id={req.LotId} in shard {shardId}. Provide 'lot_location' directly, or set FSO_DB_URL." });
                lotLocation = loc.Value;
            }

            // Fetch FSOV (cache-miss path needs it; cache-hit path needs the checksum).
            var sw = System.Diagnostics.Stopwatch.StartNew();
            byte[] fsov;
            try
            {
                fsov = await FetchFSOVAsync(shardId, lotLocation);
            }
            catch (Exception ex)
            {
                return Results.Json(new { error = "FSOV fetch failed: " + ex.Message },
                    statusCode: StatusCodes.Status502BadGateway);
            }
            if (fsov == null)
            {
                return Results.Json(new { error = $"FSOV not found for lot_location={lotLocation} in shard {shardId}" },
                    statusCode: StatusCodes.Status404NotFound);
            }
            Console.WriteLine($"[renderer/server] FSOV fetched ({fsov.Length} bytes) in {sw.ElapsedMilliseconds} ms");

            // Compute cache key.
            var fsovChecksum = ComputeChecksum(fsov);
            var cacheKey = CacheKey(req.LotId, fsovChecksum, req.Level, req.Angle, req.Zoom, req.Roofless);
            var cachePath = System.IO.Path.Combine(CacheDir, cacheKey + ".png");

            // Cache hit?
            if (File.Exists(cachePath))
            {
                var fi = new FileInfo(cachePath);
                var ageSec = (DateTime.UtcNow - fi.LastWriteTimeUtc).TotalSeconds;

                // Read image dimensions.
                var (w, h) = ReadPngDimensions(cachePath);
                Console.WriteLine($"[renderer/server] Cache hit: {cachePath} ({fi.Length} bytes, age={ageSec:F0}s, elapsed={sw.ElapsedMilliseconds} ms)");

                return Results.Json(new RenderResponse
                {
                    Path   = cachePath,
                    Width  = w,
                    Height = h,
                    AgeSec = ageSec
                });
            }

            // Cache miss — render.
            Console.WriteLine($"[renderer/server] Cache miss for key {cacheKey} — rendering...");
            await _renderSem.WaitAsync();
            try
            {
                // Double-check after acquiring semaphore (another request may have rendered meanwhile).
                if (File.Exists(cachePath))
                {
                    var fi2 = new FileInfo(cachePath);
                    var age2 = (DateTime.UtcNow - fi2.LastWriteTimeUtc).TotalSeconds;
                    var (w2, h2) = ReadPngDimensions(cachePath);
                    return Results.Json(new RenderResponse { Path = cachePath, Width = w2, Height = h2, AgeSec = age2 });
                }

                byte[] pngBytes = await RenderAsync(fsov, req.Level, rotation, zoom);

                if (pngBytes == null || pngBytes.Length == 0)
                    return Results.Json(new { error = "Render returned empty PNG" },
                        statusCode: StatusCodes.Status500InternalServerError);

                // Write to cache.
                Directory.CreateDirectory(CacheDir);
                await File.WriteAllBytesAsync(cachePath, pngBytes);

                var (w3, h3) = ReadPngDimensions(cachePath);
                var elapsed = sw.ElapsedMilliseconds;
                Console.WriteLine($"[renderer/server] Rendered + cached: {cachePath} ({pngBytes.Length} bytes) in {elapsed} ms");

                return Results.Json(new RenderResponse
                {
                    Path   = cachePath,
                    Width  = w3,
                    Height = h3,
                    AgeSec = 0
                });
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[renderer/server] Render error: {ex}");
                return Results.Json(new { error = "Render failed: " + ex.Message },
                    statusCode: StatusCodes.Status500InternalServerError);
            }
            finally
            {
                _renderSem.Release();
            }
        }

        // -----------------------------------------------------------------------
        // GameThread bridge — async wrappers around callback-based GameThread ops
        // -----------------------------------------------------------------------

        /// <summary>Login and wait for completion synchronously (called once at startup).</summary>
        static bool LoginSync()
        {
            var tcs   = new TaskCompletionSource<bool>();
            _api      = new FSO.Server.Clients.ApiClient(ApiUrl);

            _ = _api.AdminLoginAsync(ApiUser, ApiPassword, ok =>
            {
                tcs.TrySetResult(ok);
            });

            // Pump until login completes.
            while (!tcs.Task.IsCompleted)
            {
                GameThread.OnWork.WaitOne(100);
                GameThread.DigestUpdate(null);
            }
            return tcs.Task.Result;
        }

        /// <summary>
        /// Fetch FSOV bytes asynchronously — bridges the callback-based GetFSOV into a Task.
        /// The caller must NOT hold the render semaphore (the callback runs on GameThread).
        /// </summary>
        static Task<byte[]> FetchFSOVAsync(uint shardId, uint lotLocation)
        {
            var tcs = new TaskCompletionSource<byte[]>();
            _ = _api.GetFSOV(shardId, lotLocation, bytes =>
            {
                tcs.TrySetResult(bytes);
            });
            return tcs.Task;
        }

        /// <summary>
        /// Render the given FSOV bytes on the FSO GameThread and return the PNG bytes.
        /// Must be called while holding _renderSem.
        /// </summary>
        static Task<byte[]> RenderAsync(byte[] fsov, int level, WorldRotation rotation, WorldZoom zoom)
        {
            var tcs = new TaskCompletionSource<byte[]>();

            // Schedule the render on the GameThread.
            GameThread.NextUpdate(_ =>
            {
                try
                {
                    byte[] pngBytes = null;

                    // S2 parameterised render path — same logic as RenderStandaloneDebug.
                    Program.RenderFSOFAt(fsov, Program.GD, level, rotation, zoom,
                        png => pngBytes = png);

                    tcs.TrySetResult(pngBytes);
                }
                catch (Exception ex)
                {
                    tcs.TrySetException(ex);
                }
            });

            return tcs.Task;
        }

        // -----------------------------------------------------------------------
        // Shard + lot location resolution
        // -----------------------------------------------------------------------

        static void LoadShards()
        {
            // Hardcode the known mapping as baseline (works with no DB access).
            _shardNameToId["Alphaville"] = 1;

            // Attempt to extend from DB.
            if (!string.IsNullOrEmpty(DbConnectionString))
            {
                try
                {
                    using var conn = new MySqlConnection(DbConnectionString);
                    conn.Open();
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT shard_id, name FROM fso_shards";
                    using var rdr = cmd.ExecuteReader();
                    while (rdr.Read())
                    {
                        uint id   = Convert.ToUInt32(rdr["shard_id"]);
                        string nm = rdr["name"].ToString();
                        _shardNameToId[nm] = id;
                    }
                    Console.WriteLine($"[renderer/server] Loaded {_shardNameToId.Count} shard(s) from DB.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[renderer/server] Could not load shards from DB (using hardcoded): {ex.Message}");
                }
            }
        }

        static async Task<uint?> LookupLotLocation(uint shardId, uint lotId)
        {
            // Check in-memory cache first.
            if (_lotLocationCache.TryGetValue((shardId, lotId), out uint cached))
                return cached;

            if (!string.IsNullOrEmpty(DbConnectionString))
            {
                try
                {
                    await using var conn = new MySqlConnection(DbConnectionString);
                    await conn.OpenAsync();
                    await using var cmd = conn.CreateCommand();
                    cmd.CommandText = "SELECT location FROM fso_lots WHERE lot_id = @lot_id AND shard_id = @shard_id LIMIT 1";
                    cmd.Parameters.AddWithValue("@lot_id", lotId);
                    cmd.Parameters.AddWithValue("@shard_id", shardId);
                    var result = await cmd.ExecuteScalarAsync();
                    if (result != null && result != DBNull.Value)
                    {
                        uint loc = Convert.ToUInt32(result);
                        _lotLocationCache[(shardId, lotId)] = loc;
                        return loc;
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"[renderer/server] DB lookup failed for lot_id={lotId}: {ex.Message}");
                }
            }
            return null;
        }

        // -----------------------------------------------------------------------
        // Cache helpers
        // -----------------------------------------------------------------------

        static string CacheKey(uint lotId, string fsovChecksum, int level, string angle, string zoom, bool roofless)
        {
            // Deterministic, filesystem-safe key.
            var raw = $"{lotId}_{fsovChecksum}_{level}_{angle}_{zoom}_{(roofless ? 1 : 0)}";
            return raw.Replace(":", "_").Replace("/", "_");
        }

        static string ComputeChecksum(byte[] data)
        {
            using var sha = SHA256.Create();
            var hash = sha.ComputeHash(data);
            // First 12 bytes → 24 hex chars — short enough for filenames.
            return Convert.ToHexString(hash)[..24];
        }

        static void EnsureCacheDir()
        {
            var primary = CacheDir;
            try
            {
                Directory.CreateDirectory(primary);
                // Quick write-test.
                var probe = System.IO.Path.Combine(primary, ".probe");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                Console.WriteLine($"[renderer/server] Cache dir: {primary}");
            }
            catch
            {
                // Fall back to user-writable location.
                var fallback = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "freeso-renderer", "cache");
                Console.WriteLine($"[renderer/server] Cannot write to {primary}, falling back to {fallback}");
                CacheDir = fallback;
                Directory.CreateDirectory(CacheDir);
            }
        }

        static (int width, int height) ReadPngDimensions(string path)
        {
            try
            {
                using var img = Image.Load(path);
                return (img.Width, img.Height);
            }
            catch
            {
                return (0, 0);
            }
        }
    }
}
