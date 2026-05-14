// FSO.LotRenderer — Linux-portable headless lot renderer.
// Port of FSOFacadeWorker/Program.cs RenderFSOF, targeting net9.0 (not net9.0-windows).
//
// Usage:
//   FSO_RENDERER_USER=baron FSO_RENDERER_PASS=<password> \
//   SDL_VIDEODRIVER=offscreen freeso-renderer \
//     --api-url http://workshop:9000 \
//     --debug-lot 16318812 --level 1 --angle iso-ne --zoom far \
//     --out /tmp/lot2-test.png
//
// Note: --debug-lot expects the packed lot LOCATION (MapCoordinates.Pack(x,y) = x<<16|y),
//       NOT the lot_id. Lot 2 (baron's Main) is at X=249, Y=348 → location 16318812.
//
// Or under xvfb-run:
//   xvfb-run -a freeso-renderer --debug-lot 16318812 --out /tmp/lot2-test.png
//
// Required env or flags:
//   FSO_RENDERER_API_URL   (or --api-url)    e.g. http://workshop:9000
//   FSO_RENDERER_USER      (or --user)       admin username
//                           Prefer env over --user so the value does not appear in ps/proc.
//   FSO_RENDERER_PASS      (or --password)   admin password
//                           ALWAYS use env — never pass via --password in production;
//                           --password is intentionally left without a default so the
//                           process fails loudly if neither env nor flag is supplied.
//   FSO_GAME_LOCATION      (or --game-path)  path to TSOClient assets dir
//                           default: /home/baron/projects/freeso-experiment/GameAssets/TSOClient/
//
// S2 flags (per-floor / per-angle / per-zoom):
//   --level N        Floor to render (0 = terrain only, 1 = ground floor, …, max = bp.Stories)
//                    Default: bp.Stories (top floor — same as GetLotThumb default).
//   --angle <val>    Isometric camera angle. Choices: iso-ne, iso-nw, iso-se, iso-sw
//                    Default: iso-ne  (TopLeft — same as GetLotThumb default).
//   --zoom  <val>    Zoom level. Choices: far (576×576), med (576×576), near (1024×1024)
//                    Default: far  (same as GetLotThumb default).
//
// When --level / --angle / --zoom are all at their defaults the output is identical to
// what GetLotThumb produced in S1, so existing tooling is not affected.

using FSO.Common;
using FSO.Common.Rendering.Framework;
using FSO.Common.Utils;
using FSO.Files.RC;
using FSO.LotView;
using FSO.LotView.Facade;
using FSO.Server.Clients;
using FSO.SimAntics;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay.Drivers;
using Microsoft.Xna.Framework.Graphics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace FSO.LotRenderer
{
    class Program
    {
        // Populated from args / env.
        static string ApiUrl;
        static string ApiUser;
        static string ApiPassword;
        static string GamePath;
        static uint DebugLot = 16318812; // MapCoordinates.Pack(249, 348) — baron's lot 2
        static string OutPath = "/tmp/lot2-test.png";

        // S2: per-floor / per-angle / per-zoom.
        // -1 means "use bp.Stories" (same default as GetLotThumb).
        static int            RenderLevel    = -1;
        static WorldRotation? RenderRotation = null;  // null = TopLeft (iso-ne)
        static WorldZoom?     RenderZoom     = null;  // null = Far

        // S3: HTTP service mode.
        static bool ServeMode = false;
        static int  ServePort = 9101;

        static _3DLayer Layer;
        internal static GraphicsDevice GD;
        static HeadlessGraphicsDeviceService GDS;

        static int Main(string[] args)
        {
            // Parse args.
            for (int i = 0; i < args.Length; i++)
            {
                switch (args[i])
                {
                    case "--api-url":   ApiUrl      = args[++i]; break;
                    case "--user":      ApiUser     = args[++i]; break;
                    case "--password":  ApiPassword = args[++i]; break;
                    case "--game-path": GamePath    = args[++i]; break;
                    case "--debug-lot": DebugLot    = uint.Parse(args[++i]); break;
                    case "--out":       OutPath     = args[++i]; break;
                    case "--level":
                        RenderLevel = int.Parse(args[++i]);
                        break;
                    case "--angle":
                        RenderRotation = ParseAngle(args[++i]);
                        break;
                    case "--zoom":
                        RenderZoom = ParseZoom(args[++i]);
                        break;
                    case "--serve":
                        ServeMode = true;
                        break;
                    case "--port":
                        ServePort = int.Parse(args[++i]);
                        break;
                    default:
                        Console.Error.WriteLine($"Unknown flag: {args[i]}");
                        break;
                }
            }

            ApiUrl      ??= Env("FSO_RENDERER_API_URL", "http://workshop:9000");
            ApiUser     ??= Env("FSO_RENDERER_USER",    "");
            ApiPassword ??= Env("FSO_RENDERER_PASS",    "");
            GamePath    ??= Env("FSO_GAME_LOCATION",    "/home/baron/projects/freeso-experiment/GameAssets/TSOClient/");

            // Fail-fast on missing credentials before expensive graphics/content init (freesoexperiment-015).
            // An empty FSO_RENDERER_PASS would silently proceed through full asset loading then fail
            // at AdminLoginAsync — slow and opaque.  Fail here with a clear message instead.
            if (string.IsNullOrEmpty(ApiUser))
            {
                Console.Error.WriteLine("FSO_RENDERER_USER not set. See docs/ops/renderer-deployment.md for credential setup.");
                return 2;
            }
            if (string.IsNullOrEmpty(ApiPassword))
            {
                Console.Error.WriteLine("FSO_RENDERER_PASS not set. See docs/ops/renderer-deployment.md for credential setup.");
                return 2;
            }

            // Normalize FSO_GAME_PATH: ensure trailing slash for Path.Combine to work correctly on Linux.
            // On Linux, Path.Combine treats relative FAR3 paths as absolute when the prefix lacks a separator.
            GamePath = Path.TrimEndingDirectorySeparator(GamePath) + Path.DirectorySeparatorChar;

            // --- Platform init (Linux equivalent of FSO.Windows.Program.InitWindows) ---
            FSO.Files.ImageLoaderHelpers.BitmapFunction = BitmapReader;
            FSO.Files.ImageLoaderHelpers.SavePNGFunc    = SavePNG;

            TimedReferenceController.SetMode(CacheType.PERMANENT);

            Console.WriteLine("[renderer] Locating game assets at: " + GamePath);
            if (!Directory.Exists(GamePath))
            {
                Console.Error.WriteLine("[renderer] ERROR: game path not found: " + GamePath);
                return 1;
            }

            FSOEnvironment.Enable3D    = true;
            // OGL content path (not DX — we are Linux/Mesa, not DirectX)
            // Use absolute paths so the binary works regardless of working directory.
            // NOTE: AppContext.BaseDirectory returns the process CWD on Linux for self-contained
            // binaries, not the binary directory. Use Environment.ProcessPath instead.
            var baseDir = Path.GetDirectoryName(Environment.ProcessPath)
                          ?? AppContext.BaseDirectory;
            FSOEnvironment.GFXContentDir = Path.Combine(baseDir, "Content", "OGL") + Path.DirectorySeparatorChar;
            FSOEnvironment.ContentDir    = Path.Combine(baseDir, "Content") + Path.DirectorySeparatorChar;
            FSOEnvironment.Linux         = true;
            FSOEnvironment.DirectX       = false;
            FSOEnvironment.TexCompress   = false; // skip BC compression on llvmpipe
            FSOEnvironment.GameThread    = Thread.CurrentThread;

            GraphicsModeControl.ChangeMode(FSO.LotView.Model.GlobalGraphicsMode.Full3D);
            GameThread.NoGame        = true;
            GameThread.UpdateExecuting = true;

            FSO.HIT.HITVM.Init();
            FSO.HIT.HITVM.Get().SetMasterVolume(FSO.HIT.Model.HITVolumeGroup.AMBIENCE, 0);
            FSO.HIT.HITVM.Get().SetMasterVolume(FSO.HIT.Model.HITVolumeGroup.FX,       0);
            FSO.HIT.HITVM.Get().SetMasterVolume(FSO.HIT.Model.HITVolumeGroup.MUSIC,    0);
            FSO.HIT.HITVM.Get().SetMasterVolume(FSO.HIT.Model.HITVolumeGroup.VOX,      0);
            FSO.Files.Formats.IFF.Chunks.STR.DefaultLangCode =
                FSO.Files.Formats.IFF.Chunks.STRLangCode.EnglishUS;

            // --- Graphics device (headless, no display needed with SDL_VIDEODRIVER=offscreen) ---
            Console.WriteLine("[renderer] Creating headless graphics device...");
            try
            {
                GDS = new HeadlessGraphicsDeviceService();
                GD  = GDS.GraphicsDevice;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[renderer] FATAL: Could not create graphics device:");
                Console.Error.WriteLine(ex.ToString());
                Console.Error.WriteLine();
                Console.Error.WriteLine("Tip: set SDL_VIDEODRIVER=offscreen, or run under xvfb-run -a.");
                return 2;
            }
            Console.WriteLine("[renderer] Graphics device OK: " + GD.Adapter.Description);

            // Content manager for OGL shaders.
            var services = new Microsoft.Xna.Framework.GameServiceContainer();
            var content  = new Microsoft.Xna.Framework.Content.ContentManager(services);
            content.RootDirectory = FSOEnvironment.GFXContentDir;
            services.AddService<IGraphicsDeviceService>(GDS);

            // Load Vitaboy effect (shader) — this is the top-risk step.
            Console.WriteLine("[renderer] Loading Vitaboy shader...");
            Effect vitaboyEffect;
            try
            {
                vitaboyEffect = content.Load<Effect>("Effects/Vitaboy");
                FSO.Vitaboy.Avatar.setVitaboyEffect(vitaboyEffect);
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("[renderer] ERROR: Vitaboy shader failed to load:");
                Console.Error.WriteLine(ex.ToString());
                // Report the shader failure to the campfire so orchestrator knows this is a FAIL.
                Console.Error.WriteLine("[renderer] SHADER-FAIL: Effects/Vitaboy");
                GDS.Dispose();
                return 3;
            }

            WorldConfig.Current = new WorldConfig()
            {
                LightingMode   = 3,
                SmoothZoom     = true,
                SurroundingLots = 0
            };
            DGRP3DMesh.Sync = true;

            // Content providers need several directories to enumerate (even if empty).
            // Create stub directories that the providers expect but don't ship with the renderer.
            var contentDir = Path.Combine(baseDir, "Content") + Path.DirectorySeparatorChar;
            var stubDirs = new[]
            {
                Path.Combine(contentDir, "Cities"),
                Path.Combine(contentDir, "Objects"),
                Path.Combine(contentDir, "Patch"),
                Path.Combine(contentDir, "MeshCache"),
                Path.Combine(contentDir, "MeshReplace"),
            };
            foreach (var d in stubDirs)
            {
                try { Directory.CreateDirectory(d); }
                catch { /* best-effort — ignore if read-only */ }
            }
            // UserDir = where user-writable content lives (MeshCache etc.)
            FSOEnvironment.UserDir = contentDir;

            Console.WriteLine("[renderer] Loading FSO content...");
            VMContext.InitVMConfig(false);
            FSO.Content.Content.Init(GamePath, GD);
            WorldContent.Init(services, content.RootDirectory);
            VMAmbientSound.ForceDisable = true;
            Layer = new _3DLayer();
            Layer.Initialize(GD);

            // --- S3 serve mode OR S1/S2 one-shot render ---
            if (ServeMode)
            {
                Console.WriteLine("[renderer] Starting HTTP service mode...");
                RendererServer.ApiUrl      = ApiUrl;
                RendererServer.ApiUser     = ApiUser;
                RendererServer.ApiPassword = ApiPassword;
                RendererServer.DbConnectionString = Environment.GetEnvironmentVariable("FSO_DB_URL");
                // Cache dir: env override → /var/lib/freeso-renderer/cache (with fallback inside Server)
                RendererServer.CacheDir    = Environment.GetEnvironmentVariable("FSO_RENDERER_CACHE_DIR")
                                             ?? "/var/lib/freeso-renderer/cache";
                // Bind address: env override → localhost:port
                RendererServer.BindAddress = Environment.GetEnvironmentVariable("FSO_RENDERER_BIND")
                                             ?? $"127.0.0.1:{ServePort}";
                return RendererServer.Run(ServePort);
            }

            // --- Login and render (one-shot) ---
            Console.WriteLine("[renderer] Logging in to API at: " + ApiUrl);
            return RunRenderLoop();
        }

        static int _exitCode = 0;

        static int RunRenderLoop()
        {
            bool loginSent = false;
            ApiClient api  = null;

            var loop = new Thread(() =>
            {
                api = new ApiClient(ApiUrl);
                _ = api.AdminLoginAsync(ApiUser, ApiPassword, (ok) =>
                {
                    if (!ok)
                    {
                        Console.Error.WriteLine("[renderer] Login failed.");
                        _exitCode = 4;
                        GameThread.OnWork.Set();
                        return;
                    }
                    Console.WriteLine("[renderer] Login OK.");
                    RenderStandaloneDebug(api, 1, DebugLot);
                });
                loginSent = true;
            });
            loop.IsBackground = true;
            loop.Start();

            // Pump game thread callbacks (same pattern as FSOFacadeWorker.WorkerLoop).
            int attempts = 0;
            while (true)
            {
                GameThread.OnWork.WaitOne(500);
                GameThread.DigestUpdate(null);

                if (!loginSent) continue;
                if (_exitCode != 0) break;
                if (_renderDone) break;

                if (++attempts > 600) // 5 min timeout
                {
                    Console.Error.WriteLine("[renderer] Timed out waiting for render.");
                    _exitCode = 5;
                    break;
                }
            }

            GameThread.SetKilled();
            GDS.Release();
            return _exitCode;
        }

        static bool _renderDone = false;

        static void RenderStandaloneDebug(ApiClient api, uint shard, uint lot)
        {
            Console.WriteLine($"[renderer] Fetching FSOV for lot {lot}...");
            api.GetFSOV(shard, lot, (bytes) =>
            {
                if (bytes == null)
                {
                    Console.Error.WriteLine($"[renderer] Could not fetch FSOV for lot {lot}.");
                    _exitCode = 6;
                    _renderDone = true;
                    GameThread.OnWork.Set();
                    return;
                }
                Console.WriteLine($"[renderer] Got FSOV ({bytes.Length} bytes). Rendering...");

                try
                {
                    byte[] thumbPng = null;

                    // If any of --level / --angle / --zoom were specified, use GetLotThumbAt.
                    // Otherwise fall through to RenderFSOF (which calls GetLotThumb, same as S1).
                    bool useParamRender = RenderLevel >= 0 || RenderRotation.HasValue || RenderZoom.HasValue;

                    if (useParamRender)
                    {
                        RenderFSOFAt(
                            bytes, GD,
                            level:    RenderLevel,
                            rotation: RenderRotation ?? WorldRotation.TopLeft,
                            zoom:     RenderZoom     ?? WorldZoom.Far,
                            (png) => thumbPng = png);
                    }
                    else
                    {
                        RenderFSOF(bytes, GD, compressed: true, (png) => thumbPng = png);
                    }

                    var outDir = Path.GetDirectoryName(OutPath);
                    if (!string.IsNullOrEmpty(outDir)) Directory.CreateDirectory(outDir);

                    File.WriteAllBytes(OutPath, thumbPng);
                    Console.WriteLine($"[renderer] PNG written: {OutPath} ({thumbPng.Length} bytes)");
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine("[renderer] Render failed:");
                    Console.Error.WriteLine(ex.ToString());
                    _exitCode = 7;
                }
                finally
                {
                    _renderDone = true;
                    GameThread.OnWork.Set();
                }
            });
        }

        // -----------------------------------------------------------------------
        // Core render — ported from FSOFacadeWorker/Program.cs:RenderFSOF
        // -----------------------------------------------------------------------
        public static void RenderFSOF(byte[] fsov, GraphicsDevice gd, bool compressed,
            Action<byte[]> thumbAction = null)
        {
            var marshal = new VMMarshal();
            using (var mem = new MemoryStream(fsov))
                marshal.Deserialize(new BinaryReader(mem));

            // Unset any active render target left by a previous render before Present().
            // GetLotThumb renders to an off-screen target and may leave it active on
            // the device; calling Present with an active render target throws on Mesa.
            // Mirrors the fix in RenderFSOFAt (freesoexperiment-499).
            gd.SetRenderTarget(null);
            // Present before creating render targets — Mesa fence barrier needs this.
            // (FSOFacadeWorker does this too, line 193: GD.Present() before rendering)
            gd.Present();
            var world = new World(gd);
            world.Opacity = 1;
            Layer.Add(world);

            var globalLink = new VMTSOGlobalLinkStub();
            var driver     = new VMServerDriver(globalLink);
            var vm         = new VM(new VMContext(world), driver, new VMNullHeadlineProvider());
            vm.Init();
            vm.Load(marshal);

            // Prime _2DWorldBatch.WorldCamera so Resume() doesn't NullRef under SetAllLights
            // → Force2DPredraw → RecacheWalls → _2d.Pause()/_2d.Resume(). WorldCamera is null
            // until Begin() has been called once; the normal World.Draw() loop primes it but
            // our headless path bypasses Draw(). Same fix as RenderFSOFAt (freesoexperiment-fde).
            world.State._2D.Begin(world.State.Camera2D);

            SetOutsideTime(gd, vm, world, 0.5f, false);
            world.State.PrepareLighting();

            var facade = new LotFacadeGenerator()
            {
                FLOOR_TILES    = 64,
                GROUND_SUBDIV  = 5,
                FLOOR_RES_PER_TILE = 2
            };

            SetAllLights(vm, world, 0.5f, 0);

            if (thumbAction != null)
            {
                var bigThumb = world.GetLotThumb(gd, null);
                using var stream = new MemoryStream();
                var tex = TextureUtils.Decimate(bigThumb, gd, 2, false);
                tex.SaveAsPng(stream, bigThumb.Width / 2, bigThumb.Height / 2);
                thumbAction(stream.ToArray());
                tex.Dispose();
            }

            // Run LotFacadeGenerator (wall/floor textures) — not saved in single-shot mode
            // but needed to exercise the full shader pipeline for the spike.
            facade.GetFSOF(gd, world, vm.Context.Blueprint,
                () => { SetAllLights(vm, world, 0.0f, 100); }, compressed);

            Layer.Remove(world);
            world.Dispose();
            vm.Context.Ambience.Kill();
            foreach (var ent in vm.Entities)
            {
                var threads = ent.SoundThreads;
                for (int i = 0; i < threads.Count; i++)
                    threads[i].Sound.RemoveOwner(ent.ObjectID);
                threads.Clear();
            }
        }

        // -----------------------------------------------------------------------
        // S2: parameterized render — calls GetLotThumbAt instead of GetLotThumb.
        // level=-1 means "use bp.Stories" (top floor, matching GetLotThumb default).
        //
        // Fix (ce5 — Option A): GetLotThumbAt requires WorldPlatform2D, which is only
        // set when the World is initialised in Full2D mode.  The global startup code
        // calls GraphicsModeControl.ChangeMode(Full3D) so that RenderFSOF (S1) works,
        // but that causes WorldPlatform3D to be used here and GetLotThumbAt throws.
        //
        // We temporarily switch the global mode to Full2D before creating the World
        // (the mode is consumed by World.InitDefaultGraphicsMode during vm.Load), then
        // restore Full3D afterwards.  S1 (RenderFSOF) is unaffected — it never calls
        // GetLotThumbAt and creates its own World instance in a separate call.
        // -----------------------------------------------------------------------
        public static void RenderFSOFAt(
            byte[] fsov,
            GraphicsDevice gd,
            int level,
            WorldRotation rotation,
            WorldZoom zoom,
            Action<byte[]> thumbAction = null,
            bool roofless = false)
        {
            var marshal = new VMMarshal();
            using (var mem = new MemoryStream(fsov))
                marshal.Deserialize(new BinaryReader(mem));

            // Switch to Full2D so World.InitDefaultGraphicsMode creates WorldPlatform2D,
            // which GetLotThumbAt requires.  Restored to Full3D in the finally block.
            // try/finally ensures Full3D is restored even if an exception is thrown inside
            // the render pipeline (freesoexperiment-499).
            GraphicsModeControl.ChangeMode(FSO.LotView.Model.GlobalGraphicsMode.Full2D);
            try
            {
            // Unset any active render target left by a previous render before Present().
            // GetLotThumbAt renders to an off-screen target and may leave it active on
            // the device; calling Present with an active render target throws on Mesa.
            gd.SetRenderTarget(null);
            gd.Present();
            var world = new World(gd);
            world.Opacity = 1;
            Layer.Add(world);

            var globalLink = new VMTSOGlobalLinkStub();
            var driver     = new VMServerDriver(globalLink);
            var vm         = new VM(new VMContext(world), driver, new VMNullHeadlineProvider());
            vm.Init();
            vm.Load(marshal);

            // Prime _2DWorldBatch.WorldCamera so Resume() doesn't NullRef.
            // Resume() calls Begin(this.WorldCamera); WorldCamera is null until Begin()
            // has been called at least once.  The normal World.Draw() loop calls Begin()
            // first, but our headless Force2DPredraw bypasses that loop.
            // SignalAllDirty() + Force2DPredraw (in SetAllLights) triggers RecacheWalls
            // → _2d.Pause() / _2d.Resume() — if WorldCamera is still null, Resume throws.
            world.State._2D.Begin(world.State.Camera2D);

            SetOutsideTime(gd, vm, world, 0.5f, false);
            world.State.PrepareLighting();
            SetAllLights(vm, world, 0.5f, 0);

            if (thumbAction != null)
            {
                // Resolve level: -1 → bp.Stories (same as GetLotThumb default).
                int effectiveLevel = level >= 0 ? level : vm.Context.Blueprint.Stories;

                Console.WriteLine($"[renderer] GetLotThumbAt level={effectiveLevel} rotation={rotation} zoom={zoom} roofless={roofless}");

                if (roofless)
                {
                    // Roofless mode: the rooflessCallback fires inside the WithBuffer scope
                    // (after walls+objects, before RoofComp.Draw) while the render target is
                    // still live on the GPU.  We capture the half-res PNG bytes at that point
                    // and ignore the final (with-roof) texture returned by GetLotThumbAt.
                    //
                    // GetLotThumbAt always returns a GPU RenderTarget2D (bufferTexture.Get()).
                    // Capture and dispose it to avoid a GPU texture leak on every roofless
                    // render call (freesoexperiment-a30).
                    byte[] rooflessBytes = null;
                    var roofThumb = world.GetLotThumbAt(gd, effectiveLevel, rotation, zoom,
                        rooflessCallback: (rt) =>
                        {
                            // rt is the live RenderTarget2D — valid only inside this callback.
                            using var s = new MemoryStream();
                            var decimated = TextureUtils.Decimate(rt, gd, 2, false);
                            decimated.SaveAsPng(s, rt.Width / 2, rt.Height / 2);
                            rooflessBytes = s.ToArray();
                            decimated.Dispose();
                        });
                    // Dispose the returned GPU render target; we captured what we needed
                    // in the callback above.  Not disposing leaks one RenderTarget2D per
                    // roofless render (freesoexperiment-a30).
                    roofThumb?.Dispose();
                    if (rooflessBytes != null)
                        thumbAction(rooflessBytes);
                }
                else
                {
                    var bigThumb = world.GetLotThumbAt(gd, effectiveLevel, rotation, zoom);

                    using var stream = new MemoryStream();
                    // Decimate by 2 to match the same half-res output GetLotThumb uses in RenderFSOF.
                    var tex = TextureUtils.Decimate(bigThumb, gd, 2, false);
                    tex.SaveAsPng(stream, bigThumb.Width / 2, bigThumb.Height / 2);
                    thumbAction(stream.ToArray());
                    tex.Dispose();
                    // Dispose the GPU render target returned by GetLotThumbAt — it is a
                    // Texture2D/RenderTarget2D allocated on the GPU and must be freed
                    // explicitly; otherwise each cache-miss render leaks one RT (freesoexperiment-2a1).
                    bigThumb.Dispose();
                }
            }

            Layer.Remove(world);
            world.Dispose();
            vm.Context.Ambience.Kill();
            foreach (var ent in vm.Entities)
            {
                var threads = ent.SoundThreads;
                for (int i = 0; i < threads.Count; i++)
                    threads[i].Sound.RemoveOwner(ent.ObjectID);
                threads.Clear();
            }
            }
            finally
            {
                // Restore Full3D so subsequent RenderFSOF / RenderFSOFAt calls start clean.
                // This runs even if an exception is thrown in the render pipeline (499).
                GraphicsModeControl.ChangeMode(FSO.LotView.Model.GlobalGraphicsMode.Full3D);
            }
        }

        // -----------------------------------------------------------------------
        // Angle / zoom parsers (CLI string → FSO enum)
        // -----------------------------------------------------------------------

        /// <summary>
        /// Maps ISO compass label to WorldRotation.
        /// iso-ne = TopLeft (origin tile at top-left of screen — "north-east" camera)
        /// iso-nw = TopRight
        /// iso-se = BottomLeft
        /// iso-sw = BottomRight
        /// </summary>
        public static WorldRotation ParseAngle(string s) => s.ToLowerInvariant() switch
        {
            "iso-ne" => WorldRotation.TopLeft,
            "iso-nw" => WorldRotation.TopRight,
            "iso-se" => WorldRotation.BottomLeft,
            "iso-sw" => WorldRotation.BottomRight,
            _ => throw new ArgumentException($"Unknown angle '{s}'. Valid: iso-ne, iso-nw, iso-se, iso-sw")
        };

        /// <summary>Maps zoom label to WorldZoom.</summary>
        public static WorldZoom ParseZoom(string s) => s.ToLowerInvariant() switch
        {
            "far"  => WorldZoom.Far,
            "med"  => WorldZoom.Medium,
            "near" => WorldZoom.Near,
            _ => throw new ArgumentException($"Unknown zoom '{s}'. Valid: far, med, near")
        };

        // -----------------------------------------------------------------------
        static void SetAllLights(VM vm, World world, float outsideTime, short contribution)
        {
            foreach (var light in vm.Entities.Where(x =>
                x.Object.Resource.SemiGlobal?.Iff?.Filename == "lightglobals.iff"))
            {
                light.SetValue(FSO.SimAntics.Model.VMStackObjectVariable.LightingContribution, contribution);
            }
            vm.Context.Architecture.SignalAllDirty();
            vm.Context.Architecture.Tick();
            SetOutsideTime(GD, vm, world, outsideTime, false);
        }

        static void SetOutsideTime(GraphicsDevice gd, VM vm, World world, float time, bool lightsOn)
        {
            vm.Context.Architecture.SetTimeOfDay(time);
            world.Force2DPredraw(gd);
            vm.Context.Architecture.SetTimeOfDay();
        }

        // -----------------------------------------------------------------------
        // Platform helpers (ImageSharp, no System.Drawing)
        // -----------------------------------------------------------------------
        static void SavePNG(byte[] data, int width, int height, System.IO.Stream str)
        {
            var image = new Image<Rgba32>(width, height);
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int i = (y * width + x) * 4;
                    image[x, y] = new Rgba32(data[i], data[i + 1], data[i + 2], data[i + 3]);
                }
            image.Save(str, new PngEncoder());
        }

        static Tuple<byte[], int, int> BitmapReader(System.IO.Stream str)
        {
            using var image = Image.Load<Rgba32>(str);
            int w = image.Width, h = image.Height;
            var data = new byte[w * h * 4];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    int i = (y * w + x) * 4;
                    var px = image[x, y];
                    data[i]   = px.R; data[i+1] = px.G;
                    data[i+2] = px.B; data[i+3] = px.A;
                }
            return new Tuple<byte[], int, int>(data, w, h);
        }

        static string Env(string key, string fallback) =>
            Environment.GetEnvironmentVariable(key) ?? fallback;
    }
}
