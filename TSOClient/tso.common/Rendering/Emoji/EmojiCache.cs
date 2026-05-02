using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Threading;

namespace FSO.Common.Rendering.Emoji
{
    /// <summary>
    /// GPU-resident emoji atlas, backed by a persistent on-disk PNG so subsequent
    /// launches start fully primed.
    ///
    /// All emoji bytes come from the FreeSO server (see EmojiController on the API
    /// side and the emoji_sync cog on the Discord bot side). Twemoji is the upstream
    /// for the standard set — but the bot mirrors it, so the client never talks to
    /// any third-party CDN.
    ///
    /// Lifecycle:
    ///   1. Construction tries to load <c>./fso_cache/emoji/atlas.png</c>; if the
    ///      sidecar <c>index.json</c>'s schema/version match, every cached emoji is
    ///      blitted into the GPU atlas in one pass and is instantly available.
    ///   2. <see cref="OnApiAvailable"/> (called from LoginRegulator after the API
    ///      URL is known) pulls <c>/userapi/emoji/atlas.png</c> + <c>atlas.json</c>
    ///      in a single round-trip. The standard set lands in one blit.
    ///   3. Discord guild customs are fetched per-emoji via
    ///      <c>/userapi/emoji/custom/{name}.png</c> on first use, stored under the
    ///      "c:" prefix in EmojiToIndex, and gated by a small semaphore.
    ///   4. Loading/error placeholders fill any cell that hasn't been satisfied yet
    ///      so the chat balloon never renders fully blank.
    ///   5. The atlas is checkpointed to disk every 30 seconds while dirty, and on
    ///      game shutdown via <see cref="GameThread.KilledEvent"/>.
    /// </summary>
    public class EmojiCache
    {
        // FreeSO API base, set by EmojiProvider.OnApiAvailable once login resolves
        // ApiClient.CDNUrl. All emoji HTTP traffic goes here — Twemoji CDN is no
        // longer touched at runtime.
        public string ApiBase;
        public int DefaultRes = 24;
        // 64×64 = 4,096 cells at 24px each → 1,536×1,536 atlas. The full Twemoji 14.0.2
        // set is ~1,870 entries plus a small set of Discord guild customs, so we have
        // ~2× headroom.
        public int Width = 64;

        // Bump if the on-disk format/atlas-layout changes — invalidates cached PNGs.
        // v2: switched origin from Twemoji CDN to the FreeSO server atlas endpoint.
        // v3: emojis.json regenerated with Discord-aligned shortcodes (gemoji set);
        //     atlas slot ordering shifted, so the cached PNG is no longer valid.
        private const string DiskCacheVersion = "3";
        private const string TwemojiVersion = "14.0.2";
        private const long SaveIntervalMs = 30000;
        // Throttle parallel custom-emoji fetches — bulk standard set comes in one
        // round-trip via the atlas endpoint, but customs are pulled lazily.
        private const int MaxConcurrentDownloads = 4;
        // Shared with EmojiProvider, which writes "c:"+name into the dictionary so
        // GetEmoji's URL resolver routes the lookup to /userapi/emoji/custom/{name}.png.
        public const string CustomEmojiPrefix = "c:";

        public int NextIndex = 0;
        public Dictionary<string, int> EmojiToIndex = new Dictionary<string, int>();
        public RenderTarget2D EmojiTex;
        public SpriteBatch EmojiBatch;

        private GraphicsDevice GD;
        private bool atlasCleared;
        private int dirtySinceLastSave;
        private bool diskLoadAttempted;
        private bool preloadStarted;
        private bool shutdownHookInstalled;
        private GameThreadInterval saveTimer;

        // Loading/error fallback colors. Faint gray for in-flight downloads (so the
        // user sees *something* during the network round-trip), red for permanently
        // failed downloads (so they know the slot won't recover).
        private static readonly Color LoadingFill = new Color(180, 180, 180, 96);
        private static readonly Color LoadingDot  = new Color(120, 120, 120, 200);
        private static readonly Color ErrorFill   = new Color(180,  60,  60, 140);
        private static readonly Color ErrorDot    = new Color(255, 230, 230, 240);

        // Bound the number of HTTP requests in flight at once. Static because the cache
        // is a singleton and we want a single global rate-limiter even if it weren't.
        private static readonly SemaphoreSlim DownloadGate = new SemaphoreSlim(MaxConcurrentDownloads, MaxConcurrentDownloads);

        // ./fso_cache/emoji/ — sibling of the existing FileSystemCache root, so it's
        // already excluded from client-update sync (see scripts/update-client.sh).
        private static string CacheDir => Path.Combine(".", "fso_cache", "emoji");
        private static string AtlasFile => Path.Combine(CacheDir, "atlas.png");
        private static string IndexFile => Path.Combine(CacheDir, "index.json");

        private class DiskIndex
        {
            public string version;
            public string twemoji_version;
            public int atlas_grid;
            public int cell_size;
            public Dictionary<string, int> cells;
        }

        public EmojiCache(GraphicsDevice gd)
        {
            GD = gd;
            EmojiBatch = new SpriteBatch(gd);

            EmojiTex = new RenderTarget2D(gd, Width * DefaultRes, Width * DefaultRes, false, SurfaceFormat.Color, DepthFormat.None, 0, RenderTargetUsage.PreserveContents);

            // A new RenderTarget2D's contents are undefined. Clear immediately so
            // any cell sampled before its emoji loads reads as transparent rather
            // than uninitialized GPU memory.
            try { EnsureAtlasCleared(); } catch { /* device not ready yet — lazy-cleared on first stamp */ }

            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11;

            // Try to populate from the on-disk atlas right away. Failures (missing
            // file, schema mismatch, corrupt PNG) just leave us in the empty-atlas
            // state — OnApiAvailable will then refetch the bulk atlas from the
            // FreeSO server when login completes.
            TryLoadFromDisk();

            InstallSaveTimer();
        }

        /// <summary>
        /// Called by <see cref="EmojiProvider"/> once the FreeSO API URL is known
        /// (after login). Kicks off a one-shot bulk download of the standard atlas
        /// from <c>{apiBase}/userapi/emoji/atlas.{png,json}</c>.
        ///
        /// Safe to call multiple times: subsequent calls only refetch if the URL
        /// changes or the atlas hasn't yet been loaded for this session.
        /// </summary>
        public void OnApiAvailable(string apiBase)
        {
            if (string.IsNullOrEmpty(apiBase)) return;
            apiBase = apiBase.TrimEnd('/');
            var changed = ApiBase != apiBase;
            ApiBase = apiBase;
            if (preloadStarted && !changed) return;
            preloadStarted = true;

            ThreadPool.QueueUserWorkItem(_ => FetchBulkAtlas(apiBase));
        }

        // Pulls atlas.png + atlas.json from the server, blits the whole atlas in
        // one operation, and registers every standard codepoint in EmojiToIndex.
        // Subsequent uses of standard emojis are instant — no per-emoji HTTP.
        private void FetchBulkAtlas(string apiBase)
        {
            byte[] atlasBytes;
            string indexJson;
            try
            {
                using (var client = new WebClient())
                {
                    indexJson = client.DownloadString(apiBase + "/userapi/emoji/atlas.json");
                    atlasBytes = client.DownloadData(apiBase + "/userapi/emoji/atlas.png");
                }
            }
            catch
            {
                // Server unavailable — disk cache (if present) is still in effect,
                // and per-emoji fetches will still try as users encounter them.
                return;
            }

            DiskIndex idx;
            try { idx = JsonConvert.DeserializeObject<DiskIndex>(indexJson); }
            catch { return; }
            if (idx == null || idx.cells == null) return;
            // The server's atlas dimensions must match ours — if the bot was built
            // with different ATLAS_GRID/CELL_SIZE we can't blit without rescaling.
            if (idx.atlas_grid != Width || idx.cell_size != DefaultRes) return;

            GameThread.NextUpdate(_ =>
            {
                Texture2D loaded = null;
                try
                {
                    using (var mem = new MemoryStream(atlasBytes))
                        loaded = Texture2D.FromStream(GD, mem);
                    if (loaded.Width != Width * DefaultRes || loaded.Height != Width * DefaultRes)
                        return;

                    EnsureAtlasCleared();
                    GD.SetRenderTarget(EmojiTex);
                    EmojiBatch.Begin(blendState: BlendState.Opaque, sortMode: SpriteSortMode.Immediate);
                    EmojiBatch.Draw(loaded, Vector2.Zero, Color.White);
                    EmojiBatch.End();
                    GD.SetRenderTarget(null);

                    // Standard codepoints (slots 0..N-1) come exclusively from
                    // the server's atlas.json. We always trust the server's
                    // slot mapping for these — any disk-cached mapping that
                    // disagrees gets overwritten, since the atlas blit just
                    // overwrote the underlying pixels too. Custom emojis live
                    // at slot N+, allocated lazily by GetEmoji.
                    lock (EmojiToIndex)
                    {
                        foreach (var kv in idx.cells)
                            EmojiToIndex[kv.Key] = kv.Value;
                        if (idx.cells.Count > 0)
                            NextIndex = Math.Max(NextIndex, idx.cells.Values.Max() + 1);
                    }
                    Interlocked.Increment(ref dirtySinceLastSave);
                }
                catch
                {
                    // Bad PNG / GraphicsDevice transient — leave whatever's in the
                    // atlas alone; next OnApiAvailable call will retry.
                }
                finally
                {
                    loaded?.Dispose();
                }
            });
        }

        public Rectangle GetEmoji(string emojiID)
        {
            int index;
            lock (EmojiToIndex)
            {
                if (EmojiToIndex.TryGetValue(emojiID, out index))
                    return RectForIndex(index);

                if (NextIndex >= Width * Width)
                {
                    // Atlas full — return a deterministic rect for the overflow case
                    // so we don't NRE. Nothing renders here, but we keep working.
                    return new Rectangle(0, 0, DefaultRes, DefaultRes);
                }

                index = NextIndex++;
                EmojiToIndex[emojiID] = index;
            }

            // Bare codepoint (e.g. "1f600") that wasn't in the bulk atlas. The server
            // doesn't expose per-codepoint endpoints — those are bot-built into the
            // atlas. Stamp the error placeholder synchronously instead of burning a
            // network round-trip on a guaranteed 404. Customs ("c:" prefix) and
            // legacy direct URLs ("!" prefix) still go through QueueDownload.
            bool isCustom = emojiID.StartsWith(CustomEmojiPrefix, StringComparison.Ordinal);
            bool isLegacyUrl = emojiID.Length > 0 && emojiID[0] == '!';
            if (!isCustom && !isLegacyUrl)
            {
                GameThread.NextUpdate(_ => StampPlaceholder(index, ErrorFill, ErrorDot));
                return RectForIndex(index);
            }

            GameThread.NextUpdate(_ => StampPlaceholder(index, LoadingFill, LoadingDot));
            QueueDownload(emojiID, index);

            return RectForIndex(index);
        }

        // Background download → render-thread blit. Gated by DownloadGate.
        // Caller (GetEmoji) guarantees emojiID starts with "c:" (Discord guild
        // custom) or "!" (legacy direct URL). Bare codepoints are handled
        // synchronously in GetEmoji because the server has no per-codepoint
        // endpoint — they only ever land via the bulk atlas.
        private void QueueDownload(string emojiID, int index)
        {
            ThreadPool.QueueUserWorkItem(_ =>
            {
                DownloadGate.Wait();
                try
                {
                    string url;
                    if (emojiID.StartsWith(CustomEmojiPrefix, StringComparison.Ordinal))
                    {
                        if (string.IsNullOrEmpty(ApiBase))
                        {
                            // Pre-login custom-emoji request — mark error and bail.
                            MarkError(index);
                            return;
                        }
                        url = ApiBase + "/userapi/emoji/custom/"
                            + emojiID.Substring(CustomEmojiPrefix.Length) + ".png";
                    }
                    else
                    {
                        // Legacy "!url" — direct URL, used by customemojis.json fallback.
                        url = emojiID.Substring(1);
                    }

                    byte[] data;
                    try
                    {
                        using (var client = new WebClient())
                            data = client.DownloadData(url);
                    }
                    catch
                    {
                        MarkError(index);
                        return;
                    }
                    if (data == null || data.Length == 0)
                    {
                        MarkError(index);
                        return;
                    }

                    // Renamed to `state` (instead of the conventional `_`) because
                    // the enclosing ThreadPool.QueueUserWorkItem lambda already
                    // captures `_` as its own parameter, and newer C# rejects the
                    // nested shadow under discard-pattern rules.
                    GameThread.NextUpdate(state =>
                    {
                        try
                        {
                            using (var mem = new MemoryStream(data))
                            using (var raw = Texture2D.FromStream(GD, mem))
                            using (var decimated = TextureUtils.Decimate(raw, GD, 72 / DefaultRes, true))
                            {
                                BlitEmoji(index, decimated);
                            }
                            Interlocked.Increment(ref dirtySinceLastSave);
                        }
                        catch
                        {
                            MarkError(index);
                        }
                    });
                }
                finally
                {
                    DownloadGate.Release();
                }
            });
        }

        private void MarkError(int index)
        {
            GameThread.NextUpdate(_ => StampPlaceholder(index, ErrorFill, ErrorDot));
        }

        // Final blit of a downloaded emoji into its slot. Wipes the loading
        // placeholder underneath first so transparent pixels don't composite the
        // gray dot through the new image.
        private void BlitEmoji(int index, Texture2D texture)
        {
            EnsureAtlasCleared();
            var rect = RectForIndex(index);
            GD.SetRenderTarget(EmojiTex);
            EmojiBatch.Begin(blendState: BlendState.Opaque, sortMode: SpriteSortMode.Immediate);
            EmojiBatch.Draw(TextureGenerator.GetPxWhite(GD), rect, Color.TransparentBlack);
            EmojiBatch.End();
            EmojiBatch.Begin(blendState: BlendState.NonPremultiplied, sortMode: SpriteSortMode.Immediate);
            EmojiBatch.Draw(texture, rect, Color.White);
            EmojiBatch.End();
            GD.SetRenderTarget(null);
        }

        private void EnsureAtlasCleared()
        {
            if (atlasCleared) return;
            GD.SetRenderTarget(EmojiTex);
            GD.Clear(Color.TransparentBlack);
            GD.SetRenderTarget(null);
            atlasCleared = true;
        }

        // Inset filled square + centered dot, drawn purely with a 1×1 white pixel
        // so we don't need any extra textures.
        private void StampPlaceholder(int index, Color fill, Color dot)
        {
            try
            {
                EnsureAtlasCleared();
                var rect = RectForIndex(index);
                var inset = Math.Max(2, DefaultRes / 6);
                var dotSize = Math.Max(4, DefaultRes / 3);
                var dotRect = new Rectangle(
                    rect.X + (rect.Width - dotSize) / 2,
                    rect.Y + (rect.Height - dotSize) / 2,
                    dotSize, dotSize);
                var inner = new Rectangle(rect.X + inset, rect.Y + inset, rect.Width - inset * 2, rect.Height - inset * 2);

                var px = TextureGenerator.GetPxWhite(GD);
                GD.SetRenderTarget(EmojiTex);
                EmojiBatch.Begin(blendState: BlendState.Opaque, sortMode: SpriteSortMode.Immediate);
                EmojiBatch.Draw(px, rect, Color.TransparentBlack);
                EmojiBatch.End();
                EmojiBatch.Begin(blendState: BlendState.NonPremultiplied, sortMode: SpriteSortMode.Immediate);
                EmojiBatch.Draw(px, inner, fill);
                EmojiBatch.Draw(px, dotRect, dot);
                EmojiBatch.End();
                GD.SetRenderTarget(null);
            }
            catch
            {
                // Drawing during device reset — safe to skip; tracking is unaffected.
            }
        }

        private Rectangle RectForIndex(int index)
        {
            return new Rectangle((index % Width) * DefaultRes, (index / Width) * DefaultRes, DefaultRes, DefaultRes);
        }

        // ────────────────────── disk persistence ──────────────────────

        private void TryLoadFromDisk()
        {
            if (diskLoadAttempted) return;
            diskLoadAttempted = true;

            try
            {
                if (!File.Exists(AtlasFile) || !File.Exists(IndexFile)) return;

                DiskIndex idx;
                try { idx = JsonConvert.DeserializeObject<DiskIndex>(File.ReadAllText(IndexFile)); }
                catch { return; }

                if (idx == null
                    || idx.version != DiskCacheVersion
                    || idx.twemoji_version != TwemojiVersion
                    || idx.atlas_grid != Width
                    || idx.cell_size != DefaultRes
                    || idx.cells == null)
                    return;

                Texture2D loaded;
                using (var fs = File.OpenRead(AtlasFile))
                    loaded = Texture2D.FromStream(GD, fs);

                try
                {
                    if (loaded.Width != Width * DefaultRes || loaded.Height != Width * DefaultRes)
                        return;

                    EnsureAtlasCleared();
                    GD.SetRenderTarget(EmojiTex);
                    EmojiBatch.Begin(blendState: BlendState.Opaque, sortMode: SpriteSortMode.Immediate);
                    EmojiBatch.Draw(loaded, Vector2.Zero, Color.White);
                    EmojiBatch.End();
                    GD.SetRenderTarget(null);

                    lock (EmojiToIndex)
                    {
                        foreach (var kv in idx.cells)
                            EmojiToIndex[kv.Key] = kv.Value;
                        NextIndex = idx.cells.Count == 0 ? 0 : (idx.cells.Values.Max() + 1);
                    }
                }
                finally
                {
                    loaded.Dispose();
                }
            }
            catch
            {
                // Corrupt cache, IO error — ignore and start fresh; OnApiAvailable
                // will refetch the bulk atlas from the server when login completes.
            }
        }

        private void InstallSaveTimer()
        {
            // Periodic checkpoint so we don't lose a freshly-built atlas if the user
            // quits before the shutdown hook gets a chance to flush.
            if (saveTimer == null)
                saveTimer = GameThread.SetInterval(SaveAsync, SaveIntervalMs);

            if (!shutdownHookInstalled)
            {
                shutdownHookInstalled = true;
                // KilledEvent fires from OnExiting on the game thread, so we can
                // safely touch the GraphicsDevice from here. Synchronous so the
                // process doesn't tear down before the file write finishes.
                GameThread.KilledEvent += SaveSync;
            }
        }

        // Periodic save: snapshot pixels on the game thread (GetData requires the
        // GL context), then encode and write the PNG on a worker so we don't hitch
        // the frame. We deliberately do NOT round-trip through a staging Texture2D
        // — Texture2D.SaveAsPng calls GetData internally, which on Linux/mono
        // SIGSEGVs in libGLdispatch when invoked off the main thread. The pixel
        // array we already have in managed memory is all the encoder needs.
        private void SaveAsync()
        {
            if (Volatile.Read(ref dirtySinceLastSave) == 0) return;

            Color[] pixels;
            int w, h;
            Dictionary<string, int> snapshot;
            try
            {
                w = EmojiTex.Width;
                h = EmojiTex.Height;
                pixels = new Color[w * h];
                EmojiTex.GetData(pixels);
                lock (EmojiToIndex)
                    snapshot = new Dictionary<string, int>(EmojiToIndex);
            }
            catch
            {
                return;
            }
            // Reset dirty *before* the async write — if a new emoji lands while we're
            // encoding, the next tick's save will pick it up (rather than us clobbering
            // its dirty mark on completion).
            Interlocked.Exchange(ref dirtySinceLastSave, 0);

            ThreadPool.QueueUserWorkItem(_ =>
            {
                try { WriteAtlas(pixels, w, h, snapshot); }
                catch { /* disk full / permission denied — try again next tick */ }
            });
        }

        // Shutdown save: synchronous, runs on the game thread from KilledEvent.
        private void SaveSync()
        {
            if (Volatile.Read(ref dirtySinceLastSave) == 0) return;
            try
            {
                var w = EmojiTex.Width;
                var h = EmojiTex.Height;
                var pixels = new Color[w * h];
                EmojiTex.GetData(pixels);
                Dictionary<string, int> snapshot;
                lock (EmojiToIndex)
                    snapshot = new Dictionary<string, int>(EmojiToIndex);
                WriteAtlas(pixels, w, h, snapshot);
                Interlocked.Exchange(ref dirtySinceLastSave, 0);
            }
            catch { /* best-effort on shutdown */ }
        }

        // Encodes the pixel array to PNG and writes the sidecar index. Atomic-ish:
        // writes to .tmp paths first, then renames so a torn write leaves the previous
        // good cache in place.
        private void WriteAtlas(Color[] pixels, int w, int h, Dictionary<string, int> cells)
        {
            Directory.CreateDirectory(CacheDir);
            var tmpAtlas = AtlasFile + ".tmp";
            var tmpIndex = IndexFile + ".tmp";

            using (var fs = File.Create(tmpAtlas))
                WritePng(fs, pixels, w, h);

            var idx = new DiskIndex
            {
                version = DiskCacheVersion,
                twemoji_version = TwemojiVersion,
                atlas_grid = Width,
                cell_size = DefaultRes,
                cells = cells,
            };
            File.WriteAllText(tmpIndex, JsonConvert.SerializeObject(idx));

            if (File.Exists(AtlasFile)) File.Delete(AtlasFile);
            File.Move(tmpAtlas, AtlasFile);
            if (File.Exists(IndexFile)) File.Delete(IndexFile);
            File.Move(tmpIndex, IndexFile);
        }

        // ── Minimal CPU-only PNG encoder ─────────────────────────────────────────
        // Writes a single-IDAT 8-bit RGBA PNG from a Color[] without touching any
        // GPU resources. Replaces Texture2D.SaveAsPng so the save can run on a
        // background thread on Linux (where GL is main-thread-only).
        //
        // Uses filter type 0 (None) on every scanline — no per-pixel prediction.
        // That costs us a few hundred KB of compression vs MonoGame's filter
        // selection but keeps the encoder tiny and predictable.

        private static readonly byte[] PngSignature =
            { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };

        private static void WritePng(Stream output, Color[] pixels, int width, int height)
        {
            output.Write(PngSignature, 0, PngSignature.Length);

            // IHDR: width, height, bit depth=8, color type=6 (RGBA), zero compression/filter/interlace.
            var ihdr = new byte[13];
            WriteBeUInt32(ihdr, 0, (uint)width);
            WriteBeUInt32(ihdr, 4, (uint)height);
            ihdr[8] = 8; ihdr[9] = 6; ihdr[10] = 0; ihdr[11] = 0; ihdr[12] = 0;
            WriteChunk(output, "IHDR", ihdr, 0, ihdr.Length);

            // Build the filtered scanline buffer: one byte filter (0 = None) + RGBA per pixel.
            int row = width * 4;
            var raw = new byte[(row + 1) * height];
            int p = 0;
            for (int y = 0; y < height; y++)
            {
                raw[p++] = 0; // filter: None
                int srcRowStart = y * width;
                for (int x = 0; x < width; x++)
                {
                    var c = pixels[srcRowStart + x];
                    raw[p++] = c.R;
                    raw[p++] = c.G;
                    raw[p++] = c.B;
                    raw[p++] = c.A;
                }
            }

            // IDAT: zlib stream = 2-byte header + deflate data + 4-byte adler32 of raw.
            byte[] idat;
            using (var ms = new MemoryStream())
            {
                ms.WriteByte(0x78); ms.WriteByte(0x9C); // zlib header (deflate, default)
                using (var dfl = new DeflateStream(ms, CompressionLevel.Fastest, leaveOpen: true))
                    dfl.Write(raw, 0, raw.Length);
                uint adler = Adler32(raw);
                ms.WriteByte((byte)(adler >> 24));
                ms.WriteByte((byte)(adler >> 16));
                ms.WriteByte((byte)(adler >> 8));
                ms.WriteByte((byte)adler);
                idat = ms.ToArray();
            }
            WriteChunk(output, "IDAT", idat, 0, idat.Length);

            WriteChunk(output, "IEND", new byte[0], 0, 0);
        }

        private static void WriteChunk(Stream output, string type, byte[] data, int offset, int length)
        {
            var lenBuf = new byte[4];
            WriteBeUInt32(lenBuf, 0, (uint)length);
            output.Write(lenBuf, 0, 4);

            var typeBuf = new byte[] { (byte)type[0], (byte)type[1], (byte)type[2], (byte)type[3] };
            output.Write(typeBuf, 0, 4);

            if (length > 0) output.Write(data, offset, length);

            uint crc = Crc32(typeBuf, 0, 4);
            crc = Crc32(data, offset, length, crc);
            var crcBuf = new byte[4];
            WriteBeUInt32(crcBuf, 0, crc);
            output.Write(crcBuf, 0, 4);
        }

        private static void WriteBeUInt32(byte[] buf, int offset, uint v)
        {
            buf[offset    ] = (byte)(v >> 24);
            buf[offset + 1] = (byte)(v >> 16);
            buf[offset + 2] = (byte)(v >> 8);
            buf[offset + 3] = (byte)v;
        }

        private static readonly uint[] Crc32Table = BuildCrc32Table();
        private static uint[] BuildCrc32Table()
        {
            var t = new uint[256];
            for (uint i = 0; i < 256; i++)
            {
                uint c = i;
                for (int k = 0; k < 8; k++) c = ((c & 1) != 0) ? (0xEDB88320 ^ (c >> 1)) : (c >> 1);
                t[i] = c;
            }
            return t;
        }

        private static uint Crc32(byte[] data, int offset, int length, uint seed = 0xFFFFFFFF)
        {
            uint c = seed;
            for (int i = 0; i < length; i++) c = Crc32Table[(c ^ data[offset + i]) & 0xFF] ^ (c >> 8);
            return c ^ 0xFFFFFFFF;
        }

        private static uint Adler32(byte[] data)
        {
            const uint MOD = 65521;
            uint a = 1, b = 0;
            for (int i = 0; i < data.Length; i++)
            {
                a = (a + data[i]) % MOD;
                b = (b + a) % MOD;
            }
            return (b << 16) | a;
        }
    }
}