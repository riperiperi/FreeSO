using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FSO.BrowserAries;
using FSO.BrowserContent;
using FSO.Common;
using FSO.Common.Rendering.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Content.Model;
using FSO.LotView;
using FSO.LotView.Components;
using FSO.LotView.Model;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FSO_BrowserClient
{
    /// <summary>
    /// KNI/BlazorGL spike: HTTP texture + Aries city→lot join + isometric lot placeholder.
    /// S3: prefer <c>Content.Load&lt;Effect&gt;("Effects/colorpoly2D")</c> (KNIF from
    /// FSO.BrowserEffects); fall back to <see cref="BasicEffect"/>. Stock FreeSO MGFX 11
    /// probe via <c>?effect=1</c> (sample-content).
    /// S5: <c>?lot=real</c> attempts <see cref="ExternalWorld"/> + flat grass terrain;
    /// <c>?lot=1</c> stays diamond placeholder (also the failure fallback).
    /// </summary>
    public class FSO_BrowserClientGame : Microsoft.Xna.Framework.Game
    {
        static readonly Color ClearBlue = new Color(15, 18, 32);
        static readonly Color AccentBlue = new Color(79, 110, 247);
        static readonly Color PanelBlue = new Color(24, 28, 48);
        static readonly Color LabelBlue = new Color(140, 170, 255);
        static readonly Color ErrorRed = new Color(220, 80, 90);
        static readonly Color OkGreen = new Color(62, 207, 142);
        // LotTypeGrassInfo GRASS (tso.world/Model/LotTypes.cs)
        static readonly Color GrassLight = new Color(80, 116, 59);
        static readonly Color GrassDark = new Color(8, 52, 8);
        static readonly Color GrassEdge = new Color(40, 72, 28);
        static readonly Color HousePad = new Color(157, 117, 65);

        const int LotSize = 16;
        const int TileHalfW = 18;
        const int TileHalfH = 9;
        const int RealLotSize = 64;

        readonly string _contentBaseUrl;
        readonly string _gatewayBase;
        readonly bool _autoJoin;
        readonly bool _forceLotView;
        readonly bool _forceRealLot;
        readonly string _houseUrl;
        string pendingHouseXml;
        bool houseFetchStarted;
        bool houseApplied;
        JoinStage? lastLoggedJoinStage;
        FurnitureLayer furniture;
        bool furnitureFetchStarted;
        PackObjectLoader packs;
        bool packsFetchStarted;
        bool realFurniturePlaced;
        RealFurnitureLayer realFurniture;
        Newtonsoft.Json.Linq.JObject furnishDoc;
        bool furnishFetchStarted;
        readonly bool _furnishReal;   // ?furnish=real (default) → ObjectComponents; ?furnish=png → legacy billboards
        readonly string _initialZoom; // ?zoom=near|medium|far for deterministic probes
        readonly int _initialRotation; // ?rot=0..3
        bool qWasDown, eWasDown;
        readonly bool _probeFreeSoXnb;
        // ?vm=1 — join the shared lockstep VM (LotHostLite via gateway /sandbox):
        // content bundle → MEMFS → Content.Init(SERVER) → full local SimAntics VM.
        readonly bool _vmMode;
        readonly string _vmName;
        readonly string _wwwrootBase; // base URI for tso-content/, packs/, objects/
        VmLotClient vmClient;
        bool vmStarted;
        bool vmContentStarted;
        bool vmLotInited;
        bool vmArchApplied;
        int vmArchWaitFrames;
        bool vmLeftWasDown;
        bool followSim = true;
        /// <summary>Fired when a click lands a real TTAB pie menu:
        /// (calleeObjectID, [(optionID, name)], screenX, screenY). Index renders
        /// it as a DOM overlay.</summary>
        public event Action<short, List<(byte id, string name)>, int, int> OnPieMenu;
        /// <summary>Chat lines from the shared VM, forwarded to the DOM overlay.</summary>
        public event Action<string> OnChatLine;
        /// <summary>Fired once when the VM client starts (shows the chat overlay).</summary>
        public event Action OnVmStarted;
        /// <summary>Human-readable boot/status text for the DOM overlay; "" hides it.</summary>
        public event Action<string> OnStatusText;
        string lastPushedStatus;

        void PushStatus(string text)
        {
            if (text == lastPushedStatus) return;
            lastPushedStatus = text;
            OnStatusText?.Invoke(text);
        }

        /// <summary>DOM chat box submitted a message.</summary>
        public void SendChatFromUi(string message)
        {
            if (!string.IsNullOrWhiteSpace(message)) vmClient?.SendChat(message.Trim());
        }

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D pixel;
        Texture2D sampleTexture;
        BasicEffect basicEffect;
        Effect kniEffect;
        Microsoft.Xna.Framework.Content.ContentManager sampleContent;
        VertexPositionColor[] effectTriangle;
        string loadStatus = "loading…";
        string effectStatus = "effect: not yet";
        string freeSoXnbStatus;
        string realLotStatus;
        bool effectOk;
        bool kniEffectLoaded;
        bool loadStarted;
        bool joinStarted;
        bool spaceWasDown;
        bool realLotReady;
        bool realLotAttempted;

        ArchiveJoinDemo join;
        CancellationTokenSource joinCts;

        Vector2 lotPan;

        _3DLayer lotLayer;
        ExternalWorld realWorld;
        Blueprint realBlueprint;

        /// <param name="contentBaseUrl">Absolute URL of sample-content root.</param>
        /// <param name="gatewayBase">Gateway base (http://127.0.0.1:8087 or ws://…).</param>
        /// <param name="autoJoin">When true, start city→lot join ~1.5s after texture load.</param>
        /// <param name="forceLotView">When true (<c>?lot=1</c> or <c>?lot=real</c>), show lot floor without joining.</param>
        /// <param name="probeFreeSoXnb">When true (<c>?effect=1</c>), Content.Load stock FreeSO colorpoly2D from sample-content.</param>
        /// <param name="forceRealLot">When true (<c>?lot=real</c>), try ExternalWorld + TerrainComponent.</param>
        public FSO_BrowserClientGame(
            string contentBaseUrl,
            string gatewayBase,
            bool autoJoin = false,
            bool forceLotView = false,
            bool probeFreeSoXnb = false,
            bool forceRealLot = false,
            string houseUrl = null,
            bool furnishReal = true,
            string initialZoom = null,
            int initialRotation = -1,
            bool vmMode = false,
            string vmName = null,
            string wwwrootBase = null)
        {
            _contentBaseUrl = contentBaseUrl ?? throw new ArgumentNullException(nameof(contentBaseUrl));
            _gatewayBase = gatewayBase ?? throw new ArgumentNullException(nameof(gatewayBase));
            _houseUrl = houseUrl;
            _furnishReal = furnishReal;
            _initialZoom = initialZoom;
            _initialRotation = initialRotation;
            _vmMode = vmMode;
            _vmName = vmName ?? ("browser" + new Random().Next(1000));
            _wwwrootBase = wwwrootBase;
            _autoJoin = autoJoin;
            _forceLotView = forceLotView;
            _probeFreeSoXnb = probeFreeSoXnb;
            _forceRealLot = forceRealLot;
            graphics = new GraphicsDeviceManager(this);
            // Lot FX are SM3 (vs_3_0/ps_3_0); KNI WebGL defaults to Reach, which
            // rejects them ("Shader model 3.0 is not supported ... 'Reach'").
            graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Content.RootDirectory = "Content";
            Window.Title = "FreeSO Browser";

            // WebGL-aligned flags before any LotView / WorldContent work.
            ApplyWebGlEnvironmentEarly();
        }

        bool ShowLotFloor =>
            _forceLotView || vmLotInited || (join != null && join.Stage == JoinStage.LotJoined);

        bool DrawRealLot => realLotReady && realWorld != null;

        /// <summary>
        /// Match FSODroid / iOS WebGL constraints so WorldContent picks *iOS effect suffixes
        /// and TitleContainer can see wwwroot/Content.
        /// </summary>
        static void ApplyWebGlEnvironmentEarly()
        {
            FSOEnvironment.GLVer = 2;
            FSOEnvironment.SoftwareDepth = true;
            FSOEnvironment.UseMRT = false;
            FSOEnvironment.TexCompress = false;
            FSOEnvironment.TexCompressSupport = false;
            FSOEnvironment.DirectX = false;
            FSOEnvironment.Linux = true;
            FSOEnvironment.EnableNPOTMip = false;
            FSOEnvironment.MSAASupport = false;
            // TitleContainer root for BlazorGL = wwwroot/; effects live in wwwroot/Content/Effects/.
            FSOEnvironment.GFXContentDir = "Content";
            FSOEnvironment.ContentDir = "Content/";
            FSOEnvironment.UserDir = "Content/";
        }

        protected override void LoadContent()
        {
            spriteBatch = new SpriteBatch(GraphicsDevice);
            pixel = new Texture2D(GraphicsDevice, 1, 1);
            pixel.SetData(new[] { Color.White });

            // S3: KNIF XNB first (wwwroot/Content/Effects/); BasicEffect always for draw + fallback status.
            TryLoadKniEffectXnb();
            InitBasicEffectForDraw();
            EnsureEffectTriangle();

            if (_probeFreeSoXnb)
                ProbeFreeSoEffectXnb();

            if (_forceRealLot && !realLotAttempted)
            {
                realLotAttempted = true;
                TryInitRealLot();
            }

            if (!loadStarted)
            {
                loadStarted = true;
                _ = LoadSampleViaHttpStoreAsync();
            }
        }

        /// <summary>
        /// Best-effort empty flat-grass lot via ExternalWorld. Any failure → diamonds.
        /// </summary>
        void TryInitRealLot(int lotSize = RealLotSize)
        {
            try
            {
                FSOEnvironment.GameThread = Thread.CurrentThread;
                GameThread.NoGame = true;
                GameThread.UpdateExecuting = true;
                GameThread.Game = Thread.CurrentThread;

                WorldConfig.Current = new WorldConfig
                {
                    LightingMode = 0,
                    SurroundingLots = 0,
                    Weather = false,
                };

                WorldContent.Init(Services, FSOEnvironment.GFXContentDir);

                // The SIMPLE (iOS) shaders do software depth: they sample depthMap and
                // discard any pixel behind it. Desktop binds it per-frame via
                // PPXDepthEngine; here it's never bound, so the zero texture makes EVERY
                // pixel discard — terrain "draws" but rasterizes nothing. Bind far depth
                // (white unpacks to max) so nothing is culled until the depth pipeline
                // is wired for WebGL.
                var farDepth = new Texture2D(GraphicsDevice, 1, 1);
                farDepth.SetData(new[] { Color.White });
                WorldContent.GrassEffect.Parameters["depthMap"]?.SetValue(farDepth);
                WorldContent.RCObject.Parameters["depthMap"]?.SetValue(farDepth);
                // The 2D sprite batch (object sprites) does the same software-depth
                // discard. PPXDepthEngine only binds depthMap when ActiveDepth exists,
                // which ExternalWorld never sets up — unbound, EVERY object sprite
                // pixel is culled. Far depth = draw-order-only occlusion for now.
                WorldContent._2DWorldBatchEffect.Parameters["depthMap"]?.SetValue(farDepth);
                // Interim object pass for aliased KNIF matrices (see _2DWorldBatch
                // PrepareImmediate): with viewProjection re-asserted last, ALL aliased
                // matrix slots hold the pixel matrix, so vsZSprite's depth outputs are
                // garbage. drawZSprite + depthOutMode=true skips the software depth
                // discard and has no depth-output branch — draw-order occlusion until
                // the FX are rebuilt with distinct semantics (?objtech= overrides).
                if (FixedFx)
                {
                    // Rebuilt FX (distinct matrix semantics): vanilla engine path.
                    WorldContent._2DWorldBatchEffect.depthOutMode = false;
                }
                else
                {
                    WorldContent._2DWorldBatchEffect.depthOutMode = true;
                    if (WorldEntities.ObjectTechniqueOverride == null)
                        WorldEntities.ObjectTechniqueOverride = FSO.LotView.Effects.WorldBatchTechniques.drawZSprite;
                    FSO.LotView.Utils._2DWorldBatch.AliasedMatrixWorkaround = true;
                }

                // RC (flat-color) walls in the fixed 2D camera — the sprite wall path
                // needs TSO content. Light factors normally come from LMapBatch, which
                // never runs with State.Light == null; psWallRC divides by MapLayout.
                WorldArchitecture.ForceRCWalls2D = true;
                WorldContent.RCObject.MapLayout = new Vector2(3, 2);
                WorldContent.RCObject.Parameters["WorldToLightFactor"]?.SetValue(
                    new Vector3(1f / (3 * 75), 1f / (3 * 2.95f), 1f / (3 * 75)));

                lotLayer = new _3DLayer();
                lotLayer.Initialize(GraphicsDevice);

                realWorld = new ExternalWorld(GraphicsDevice);
                lotLayer.AddExternal(realWorld);

                var size = lotSize;
                realBlueprint = new Blueprint(size, size);
                realBlueprint.BuildableArea = new Rectangle(1, 1, size - 2, size - 2);
                realBlueprint.Light = new[]
                {
                    new RoomLighting { OutsideLight = 100 },
                    new RoomLighting { OutsideLight = 100 },
                    new RoomLighting { OutsideLight = 100 },
                };
                realBlueprint.OutsideColor = Color.White;
                realBlueprint.GenerateRoomLights();

                var heights = new short[size * size];
                var grass = new byte[size * size];
                var rng = new Random(1);
                for (int i = 0; i < grass.Length; i++)
                    grass[i] = (byte)rng.Next(0, 90);

                realBlueprint.Altitude = heights;
                realBlueprint.AltitudeCenters = heights;

                var terrain = new TerrainComponent(new Rectangle(0, 0, size, size), realBlueprint);
                realBlueprint.Terrain = terrain;
                terrain.Initialize(GraphicsDevice, realWorld.State);
                terrain.UpdateTerrain(TerrainType.GRASS, TerrainType.GRASS, heights, grass);

                realWorld.InitBlueprint(realBlueprint);
                // Normally Changes' FLOOR_CHANGED dirty flag triggers this; our hand-built
                // blueprint never gets one, and without it FloorGeom has no ground tiles —
                // terrain "draws" zero primitives.
                realBlueprint.FloorGeom.FullReset(GraphicsDevice, false);
                realWorld.State.WorldSize = size;
                realWorld.State.CenterTile = new Vector2(size / 2f, size / 2f);
                if (realWorld.State.AmbientLight != null)
                    realWorld.State.AmbientLight.SetData(realBlueprint.RoomColors);
                if (realWorld.State.OutsidePx != null)
                    realWorld.State.OutsidePx.SetData(new[] { Color.White });

                // Drain any InUpdate callbacks queued during InitBlueprint.
                GameThread.DigestUpdate(new UpdateState());

                realLotReady = true;
                realLotStatus = "real LotView OK (empty grass)";
                Console.WriteLine(realLotStatus);
            }
            catch (Exception ex)
            {
                realLotReady = false;
                realLotStatus = "real LotView failed → diamonds: " + Truncate(ex.Message, 140);
                Console.WriteLine(realLotStatus);
                if (ex.InnerException != null)
                    Console.WriteLine("  inner: " + ex.InnerException.Message);
                Console.WriteLine(ex.StackTrace);
                DisposeRealLot();
            }
            finally
            {
                GameThread.UpdateExecuting = false;
            }
        }

        void DisposeRealLot()
        {
            try { realWorld?.Dispose(); } catch { /* ignore */ }
            realWorld = null;
            realBlueprint = null;
            lotLayer = null;
            realLotReady = false;
        }

        /// <summary>
        /// Load KNI-rebuilt colorpoly2D (BlazorGL KNIF). Built by PackTools/FSO.BrowserEffects
        /// on Windows/CI — not stock FreeSO MGFX 11.
        /// </summary>
        void TryLoadKniEffectXnb()
        {
            try
            {
                kniEffect = Content.Load<Effect>("Effects/colorpoly2D");
                if (kniEffect == null)
                    throw new InvalidOperationException("Content.Load returned null");
                kniEffectLoaded = true;
                effectOk = true;
                effectStatus = "effect OK (Content.Load colorpoly2D)";
                Console.WriteLine(effectStatus);
            }
            catch (Exception ex)
            {
                kniEffectLoaded = false;
                kniEffect = null;
                Console.WriteLine("KNI XNB Content.Load failed: " + Truncate(ex.Message, 120));
                if (ex.InnerException != null)
                    Console.WriteLine("  inner: " + ex.InnerException.Message);
            }
        }

        void InitBasicEffectForDraw()
        {
            try
            {
                basicEffect = new BasicEffect(GraphicsDevice)
                {
                    VertexColorEnabled = true,
                    LightingEnabled = false,
                    TextureEnabled = false,
                };
                if (!kniEffectLoaded)
                {
                    effectOk = true;
                    effectStatus = "effect OK (BasicEffect fallback)";
                }
                Console.WriteLine(kniEffectLoaded
                    ? "BasicEffect ready (draw helper; status from Content.Load)"
                    : effectStatus);
            }
            catch (Exception ex)
            {
                if (!kniEffectLoaded)
                {
                    effectOk = false;
                    effectStatus = "effect failed: " + ex.GetType().Name + ": " + Truncate(ex.Message, 120);
                }
                Console.WriteLine("BasicEffect init failed: " + ex.Message);
            }
        }

        void EnsureEffectTriangle()
        {
            if (effectTriangle != null) return;
            effectTriangle = new[]
            {
                new VertexPositionColor(new Vector3(0f, 0.35f, 0f), Color.Lime),
                new VertexPositionColor(new Vector3(-0.3f, -0.25f, 0f), Color.OrangeRed),
                new VertexPositionColor(new Vector3(0.3f, -0.25f, 0f), Color.CornflowerBlue),
            };
        }

        /// <summary>
        /// Negative test: stock FreeSO MonoGame XNB (MGFX 11) under sample-content/effects/.
        /// KNI 4.2 accepts MGFX 10 / KNIF 11–12 only.
        /// </summary>
        void ProbeFreeSoEffectXnb()
        {
            try
            {
                sampleContent ??= new Microsoft.Xna.Framework.Content.ContentManager(
                    Services, "sample-content");
                var effect = sampleContent.Load<Effect>("effects/colorpoly2D");
                freeSoXnbStatus = "FreeSO XNB unexpected OK: " + (effect?.GetType().Name ?? "null");
                Console.WriteLine(freeSoXnbStatus);
            }
            catch (Exception ex)
            {
                freeSoXnbStatus = "FreeSO XNB blocked: " + Truncate(ex.Message, 160);
                Console.WriteLine(freeSoXnbStatus);
                if (ex.InnerException != null)
                    Console.WriteLine("  inner: " + ex.InnerException.Message);
            }
        }

        async Task FetchHouseAsync()
        {
            try
            {
                using var http = new System.Net.Http.HttpClient();
                pendingHouseXml = await http.GetStringAsync(_houseUrl).ConfigureAwait(true);
                Console.WriteLine($"house xml fetched ({pendingHouseXml.Length} bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("house xml fetch failed: " + ex.Message);
            }
        }

        async Task FetchFurnishAsync(string furnishUrl)
        {
            try
            {
                using var http = new System.Net.Http.HttpClient();
                var json = await http.GetStringAsync(furnishUrl).ConfigureAwait(true);
                furnishDoc = Newtonsoft.Json.Linq.JObject.Parse(json);
            }
            catch (Exception ex)
            {
                Console.WriteLine("furnish json fetch failed: " + ex.Message);
            }
        }

        async Task LoadSampleViaHttpStoreAsync()
        {
            try
            {
                using var store = new HttpContentStore(_contentBaseUrl);
                using (var stream = await store.OpenAsync("textures/squares.png").ConfigureAwait(true))
                {
                    sampleTexture = Texture2D.FromStream(GraphicsDevice, stream);
                }
                if (_forceRealLot)
                    loadStatus = realLotReady
                        ? "texture OK — real LotView + " + effectStatus
                        : "texture OK — real LotView failed, diamonds + " + effectStatus;
                else if (_forceLotView)
                    loadStatus = "texture OK — lot + " + effectStatus;
                else if (_autoJoin)
                    loadStatus = "texture OK — auto-join shortly (Space also works)";
                else
                    loadStatus = "texture OK — press Space to join (or ?gateway=…&join=1)";
            }
            catch (Exception ex)
            {
                loadStatus = "texture failed: " + ex.GetType().Name + ": " + ex.Message;
                Console.WriteLine(loadStatus);
            }
        }

        void StartJoin()
        {
            // Forced lot view used to exclude joining; ?house= + ?join=1 now run both —
            // the AI house renders locally while the Aries join runs to LotJoined.
            if (joinStarted) return;
            joinStarted = true;
            joinCts = new CancellationTokenSource();
            join = new ArchiveJoinDemo(_gatewayBase);
            join.Changed += () => { /* status read each Draw */ };
            _ = join.RunAsync(joinCts.Token);
        }

        protected override void UnloadContent()
        {
            joinCts?.Cancel();
            DisposeRealLot();
            sampleTexture?.Dispose();
            sampleTexture = null;
            kniEffect = null;
            sampleContent?.Dispose();
            sampleContent = null;
            basicEffect?.Dispose();
            basicEffect = null;
            pixel?.Dispose();
            pixel = null;
        }

        protected override void Update(GameTime gameTime)
        {
            var keyboardState = Keyboard.GetState();
            var gamePadState = GamePad.GetState(PlayerIndex.One);

            if (keyboardState.IsKeyDown(Keys.Escape) ||
                keyboardState.IsKeyDown(Keys.Back) ||
                gamePadState.Buttons.Back == ButtonState.Pressed)
            {
                try { Exit(); }
                catch (PlatformNotSupportedException) { /* ignore */ }
            }

            var space = keyboardState.IsKeyDown(Keys.Space);
            if (space && !spaceWasDown) StartJoin();
            spaceWasDown = space;

            if (_vmMode)
            {
                if (!vmContentStarted)
                {
                    vmContentStarted = true;
                    var tarUrl = new Uri(new Uri(_wwwrootBase), "tso-content/content.tar.gz").AbsoluteUri;
                    _ = BrowserContentBoot.RunAsync(tarUrl);
                }
                loadStatus = BrowserContentBoot.Ready
                    ? (vmClient?.Status ?? "vm starting")
                    : BrowserContentBoot.Status;
                // Readable progress until the lot is on screen, then hide.
                PushStatus(vmArchApplied ? "" : loadStatus);

                if (BrowserContentBoot.Ready && !vmStarted)
                {
                    vmStarted = true;
                    vmClient = new VmLotClient(_vmName);
                    vmClient.AutoChat = $"hello from {_vmName}";
                    vmClient.OnChatLine += (line) => OnChatLine?.Invoke(line);
                    vmClient.Start(_gatewayBase);
                    _ = vmClient.LoadBillboardsAsync(GraphicsDevice, _wwwrootBase);
                    OnVmStarted?.Invoke();
                }

                vmClient?.Update(gameTime.ElapsedGameTime.TotalSeconds);

                // Keep your own sim on screen. It joins at the lot edge, far from
                // the house, and a capsule at the frame edge (or just outside it)
                // reads as "there is no sim". Any manual pan hands control back.
                if (followSim && DrawRealLot && vmClient != null)
                {
                    var tile = vmClient.MyTile();
                    if (tile.HasValue) realWorld.State.CenterTile = tile.Value;
                }

                // Clicks arrive from JS via OnCanvasClick — KNI's Mouse.GetState
                // misses synthetic (and some real) clicks under BlazorGL.

                if (vmClient != null && vmClient.Synced && !vmLotInited)
                {
                    vmLotInited = true;
                    realLotAttempted = true;
                    TryInitRealLot(vmClient.vm.Context.Architecture.Width);
                    // Let the world draw a couple of clean frames before signalling
                    // wall changes: RecacheWalls resumes the 2D batch, which needs a
                    // WorldCamera that only exists after the first Begin.
                    vmArchWaitFrames = 3;
                }
                if (vmLotInited && !vmArchApplied && realLotReady && --vmArchWaitFrames <= 0)
                {
                    vmArchApplied = true;
                    try
                    {
                        vmClient.ApplyArchitecture(realBlueprint);
                        realBlueprint.FloorGeom.FullReset(GraphicsDevice, false);
                        var centroid = BlueprintArchLoader.WallCentroid(realBlueprint);
                        realWorld.State.CenterTile = centroid;
                        realWorld.State.Zoom = _initialZoom switch
                        {
                            "near" => WorldZoom.Near,
                            "medium" => WorldZoom.Medium,
                            _ => WorldZoom.Far,
                        };
                        if (_initialRotation >= 0 && _initialRotation < 4)
                            realWorld.State.Rotation = (WorldRotation)_initialRotation;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("vm arch apply failed: " + ex.Message);
                        Console.WriteLine("  stack: " + Truncate(ex.StackTrace ?? "", 800));
                    }
                }
            }

            if (_houseUrl != null && !houseFetchStarted)
            {
                houseFetchStarted = true;
                _ = FetchHouseAsync();
            }
            if (_houseUrl != null && realLotReady && !furnitureFetchStarted)
            {
                furnitureFetchStarted = true;
                furniture = new FurnitureLayer();
                // In real-furniture mode the layer still owns the capsule sims;
                // its billboards stay as the ?furnish=png fallback.
                furniture.DrawFurniture = !_furnishReal;
                var furnishUrl = _houseUrl.Replace(".xml", "-furnish.json");
                _ = furniture.LoadAsync(GraphicsDevice, new Uri(new Uri(_houseUrl), "/").AbsoluteUri, furnishUrl);
            }
            if (_houseUrl != null && _furnishReal && realLotReady && !packsFetchStarted)
            {
                packsFetchStarted = true;
                packs = new PackObjectLoader();
                packs.ProbeDevice = GraphicsDevice;
                _ = packs.LoadAsync(new Uri(new Uri(_houseUrl), "/").AbsoluteUri);
            }
            if (_houseUrl != null && _furnishReal && !furnishFetchStarted)
            {
                furnishFetchStarted = true;
                _ = FetchFurnishAsync(_houseUrl.Replace(".xml", "-furnish.json"));
            }
            if (_furnishReal && !realFurniturePlaced && houseApplied
                && packs != null && packs.Ready && furnishDoc != null)
            {
                realFurniturePlaced = true;
                try
                {
                    realFurniture = new RealFurnitureLayer();
                    realFurniture.Place(realWorld, realBlueprint, packs, furnishDoc);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("furniture real placement failed: " + ex.Message);
                    Console.WriteLine("  stack: " + Truncate(ex.StackTrace ?? "", 800));
                }
            }
            if (pendingHouseXml != null && realLotReady && !houseApplied)
            {
                houseApplied = true;
                try
                {
                    BlueprintArchLoader.Load(realBlueprint, pendingHouseXml);
                    realBlueprint.FloorGeom.FullReset(GraphicsDevice, false);
                    // Frame the house: centre on its wall centroid, widest fixed zoom.
                    var centroid = BlueprintArchLoader.WallCentroid(realBlueprint);
                    realWorld.State.CenterTile = centroid;
                    realWorld.State.Zoom = _initialZoom switch
                    {
                        "near" => WorldZoom.Near,
                        "medium" => WorldZoom.Medium,
                        _ => WorldZoom.Far,
                    };
                    if (_initialRotation >= 0 && _initialRotation < 4)
                        realWorld.State.Rotation = (WorldRotation)_initialRotation;
                    Console.WriteLine($"house arch loaded: {realBlueprint.WallsAt[0].Count} wall tiles, centre {centroid}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("house arch load failed: " + ex.Message);
                    Console.WriteLine("  stack: " + Truncate(ex.StackTrace ?? "", 800));
                }
                pendingHouseXml = null;
            }

            if (_autoJoin && !joinStarted && sampleTexture != null
                && gameTime.TotalGameTime.TotalSeconds > 1.5)
                StartJoin();

            if (ShowLotFloor)
            {
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (DrawRealLot)
                {
                    // Pan CenterTile (tile units) for real LotView camera.
                    const float tilePan = 8f;
                    var ct = realWorld.State.CenterTile;
                    if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A)
                        || keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D)
                        || keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W)
                        || keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S))
                        followSim = false; // manual pan wins
                    if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A))
                        ct.X -= tilePan * dt;
                    if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D))
                        ct.X += tilePan * dt;
                    if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W))
                        ct.Y -= tilePan * dt;
                    if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S))
                        ct.Y += tilePan * dt;
                    realWorld.State.CenterTile = ct;

                    // Zoom 1/2/3, rotate Q/E — exercised by the sprite probes.
                    if (keyboardState.IsKeyDown(Keys.D1)) realWorld.State.Zoom = WorldZoom.Near;
                    if (keyboardState.IsKeyDown(Keys.D2)) realWorld.State.Zoom = WorldZoom.Medium;
                    if (keyboardState.IsKeyDown(Keys.D3)) realWorld.State.Zoom = WorldZoom.Far;
                    var q = keyboardState.IsKeyDown(Keys.Q);
                    var e = keyboardState.IsKeyDown(Keys.E);
                    if (q && !qWasDown)
                        realWorld.State.Rotation = (WorldRotation)(((int)realWorld.State.Rotation + 3) % 4);
                    if (e && !eWasDown)
                        realWorld.State.Rotation = (WorldRotation)(((int)realWorld.State.Rotation + 1) % 4);
                    qWasDown = q; eWasDown = e;
                }
                else
                {
                    const float panSpeed = 120f;
                    if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A))
                        lotPan.X += panSpeed * dt;
                    if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D))
                        lotPan.X -= panSpeed * dt;
                    if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W))
                        lotPan.Y += panSpeed * dt;
                    if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S))
                        lotPan.Y -= panSpeed * dt;
                }
            }

            if (DrawRealLot)
            {
                try
                {
                    GameThread.UpdateExecuting = true;
                    GameThread.DigestUpdate(new UpdateState());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("real LotView update failed → diamonds: " + Truncate(ex.Message, 120));
                    DisposeRealLot();
                    realLotStatus = "real LotView update failed → diamonds";
                }
                finally
                {
                    GameThread.UpdateExecuting = false;
                }
            }

            base.Update(gameTime);
        }

        /// <summary>Canvas click (from JS interop) → tile → nearest VM object → pie menu.</summary>
        public void OnCanvasClick(float x, float y)
        {
            if (vmClient == null || !vmClient.Synced || !DrawRealLot) return;
            var tile = realWorld.State.WorldSpace.GetTileAtPosWithScroll(new Vector2(x, y));
            RequestPieMenu(tile, (int)x, (int)y);
        }

        void RequestPieMenu(Vector2 tile, int screenX, int screenY)
        {
            try
            {
                var (target, pie) = vmClient.PieMenuAt(tile);
                if (target == null)
                {
                    Console.WriteLine($"pie: no object near tile {tile.X:F1},{tile.Y:F1}");
                    return;
                }
                var guid = target.Object?.OBJ?.GUID ?? 0;
                var items = (pie ?? new List<FSO.SimAntics.VMPieMenuInteraction>())
                    .Select(p => (p.ID, p.Name)).ToList();
                Console.WriteLine($"pie menu on 0x{guid:X8} (obj {target.ObjectID}): " +
                    (items.Count == 0 ? "(empty)" : string.Join(", ", items.Select(p => $"{p.ID}:{p.Name}"))));
                if (items.Count > 0)
                    OnPieMenu?.Invoke(target.ObjectID, items, screenX, screenY);
            }
            catch (Exception ex)
            {
                Console.WriteLine("pie menu failed: " + ex.Message);
            }
        }

        /// <summary>DOM overlay (or test hook) picked an option.</summary>
        public void SelectPieOption(short calleeID, byte interactionID)
        {
            vmClient?.SendInteraction(calleeID, interactionID);
        }

        /// <summary>Test hook: pie menu by tile coordinate, bypassing the mouse.</summary>
        public string DebugPieAt(float tileX, float tileY)
        {
            if (vmClient == null || !vmClient.Synced) return "[]";
            var (target, pie) = vmClient.PieMenuAt(new Vector2(tileX, tileY));
            if (target == null || pie == null) return "[]";
            var items = pie.Select(p => $"{{\"id\":{p.ID},\"name\":\"{p.Name?.Replace("\"", "'")}\",\"callee\":{target.ObjectID}}}");
            return "[" + string.Join(",", items) + "]";
        }

        /// <summary>Test hook: where is my sim, and is it drawable? Visual QA
        /// needs the truth about the avatar, not an inference from pixels.</summary>
        public string DebugMe()
        {
            var ava = vmClient?.MyAvatar;
            if (ava == null) return "{\"found\":false}";
            var pos = ava.Position;
            var queue = string.Join(";", ava.Thread?.Queue?.Select(q => q.Name ?? "?") ?? new string[0]);
            var screen = realWorld == null ? Vector2.Zero
                : realWorld.State.WorldSpace.GetScreenFromTile(new Vector2(pos.x / 16f, pos.y / 16f))
                  + realWorld.State.WorldSpace.GetPointScreenOffset();
            return "{\"found\":true"
                + $",\"tileX\":{pos.x / 16f:F1},\"tileY\":{pos.y / 16f:F1},\"level\":{pos.Level}"
                + $",\"outOfWorld\":{(pos == FSO.LotView.Model.LotTilePos.OUT_OF_WORLD).ToString().ToLower()}"
                + $",\"hidden\":{ava.GetValue(FSO.SimAntics.Model.VMStackObjectVariable.Hidden)}"
                + $",\"screenX\":{(int)screen.X},\"screenY\":{(int)screen.Y}"
                + $",\"queue\":\"{queue}\"}}";
        }

        /// <summary>Test hook: screen position of a tile centre (for real click tests).</summary>
        public string DebugScreenPos(float tileX, float tileY)
        {
            if (realWorld == null) return "{}";
            var space = realWorld.State.WorldSpace;
            var screen = space.GetScreenFromTile(new Vector2(tileX, tileY)) + space.GetPointScreenOffset();
            return $"{{\"x\":{(int)screen.X},\"y\":{(int)screen.Y}}}";
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(ShowLotFloor ? new Color(20, 28, 18) : ClearBlue);

            if (ShowLotFloor)
            {
                if (DrawRealLot)
                {
                    if (!TryDrawRealLot())
                    {
                        spriteBatch.Begin();
                        DrawLotPlaceholder();
                        spriteBatch.End();
                    }
                }
                else
                {
                    spriteBatch.Begin();
                    DrawLotPlaceholder();
                    spriteBatch.End();
                }

                DrawBasicEffectTriangle();

                spriteBatch.Begin();
                if (DrawRealLot && furniture != null) furniture.Draw(spriteBatch, realWorld.State);
                if (DrawRealLot && vmClient != null) vmClient.DrawEntities(spriteBatch, realWorld.State);
                DrawLotStatusStrip();
                spriteBatch.End();
            }
            else
            {
                spriteBatch.Begin();
                DrawJoinPanel();
                spriteBatch.End();
            }

            base.Draw(gameTime);
        }

        bool realLotDebugDumped;
        bool furnDrawDumped;

        /// <summary>?spritetest=1 — draw one hand-built magenta _2DStandaloneSprite
        /// through PrepareImmediate/DrawImmediate to isolate batch machinery from
        /// per-sprite DGRP data.</summary>
        public static bool SpriteTest;
        public static bool SpriteTestBasic;
        public static bool DepthOutProbe;
        /// <summary>?fx=fixed — run the vanilla effect path (rebuilt XNBs with
        /// distinct matrix semantics); off = KNIF-alias workarounds.</summary>
        public static bool FixedFx;
        /// <summary>?redraw=1 — after the world pass, re-draw the real ObjectComponents
        /// standalone with a fresh PrepareImmediate (same objects, isolated state).</summary>
        public static bool RedrawProbe;
        FSO.LotView.Utils._2DStandaloneSprite testSprite;

        void DrawSpriteTest()
        {
            var st = realWorld.State;
            if (testSprite == null)
            {
                if (st.WorldRectangle.Width == 0) return;
                var tex = new Texture2D(GraphicsDevice, 64, 64);
                var px = new Color[64 * 64];
                for (int i = 0; i < px.Length; i++) px[i] = Color.Magenta;
                tex.SetData(px);
                testSprite = new FSO.LotView.Utils._2DStandaloneSprite
                {
                    Pixel = tex,
                    RenderMode = FSO.LotView.Utils._2DBatchRenderMode.NO_DEPTH,
                    SrcRect = new Rectangle(0, 0, 64, 64),
                    DestRect = new Rectangle(0, 0, 64, 64),
                    Room = 65535,
                    Floor = 1,
                    ObjectID = 1,
                    AbsoluteWorldPosition = new Vector3(32 * 3f, 0f, 32 * 3f),
                };
                // Alpha8 — the format SPR2 Z textures actually use (GetZTexture).
                var dtex = new Texture2D(GraphicsDevice, 64, 64, false, SurfaceFormat.Alpha8);
                var dpx = new byte[64 * 64];
                for (int i = 0; i < dpx.Length; i++) dpx[i] = 255; // far depth
                dtex.SetData(dpx);
                testSprite.Depth = dtex;
                testSprite.RenderMode = FSO.LotView.Utils._2DBatchRenderMode.Z_BUFFER;
                Console.WriteLine("spritetest armed");
                var eff = WorldContent._2DWorldBatchEffect;
                var names = new System.Text.StringBuilder();
                for (int i = 0; i < eff.Techniques.Count; i++)
                    names.Append($"[{i}]{eff.Techniques[i].Name}({eff.Techniques[i].Passes.Count}p) ");
                Console.WriteLine($"fx2d techniques: {names}");
                var pnames = new System.Text.StringBuilder();
                foreach (var p in eff.Parameters) pnames.Append(p.Name).Append(' ');
                Console.WriteLine($"fx2d parameters: {pnames}");
            }
            // Track the live culling rect — the camera moves when the house applies.
            testSprite.AbsoluteDestRect = new Rectangle(
                st.WorldRectangle.Center.X - 32, st.WorldRectangle.Center.Y - 32, 64, 64);
            testSprite.PrepareVertices(GraphicsDevice);
            var rectVp = st.WorldRectangle;
            var pixelView = new Matrix(
                1f, 0f, 0f, 0f,
                0f, -1f, 0f, 0f,
                0f, 0f, -1f, 0f,
                0f, 0f, 0f, 1f);
            var pixelProj = Matrix.CreateOrthographicOffCenter(
                rectVp.X - 1.5f, rectVp.X + rectVp.Width - 1.5f,
                -rectVp.Y - rectVp.Height - 0.5f, -rectVp.Y - 0.5f, 0, 1);
            var pixelVP = pixelView * pixelProj;

            var _2d = st._2D;
            _2d.OffsetPixel(new Vector2());
            _2d.OffsetTile(new Vector3());
            _2d.PrepareImmediate(FSO.LotView.Effects.WorldBatchTechniques.drawSimple);
            // Merge hypothesis: KNIF may collapse same-semantic matrices; feed the
            // pixel ortho into all three slots for this test draw.
            WorldContent._2DWorldBatchEffect.worldViewProjection = pixelVP;
            WorldContent._2DWorldBatchEffect.rotProjection = pixelVP;
            WorldContent._2DWorldBatchEffect.iWVP = Matrix.Invert(pixelVP);
            // Depth state parity with the BasicEffect bisect draw.
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            _2d.DrawImmediate(testSprite);
            _2d.EndImmediate();

            // A: +96 — real component's textures on my quad (matrix override kept).
            // B: +192 — my textures, NO matrix override (PrepareImmediate world matrices).
            Texture2D realPix = null, realDepth = null;
            if (realBlueprint.Objects.Count > 0)
            {
                try
                {
                    var comp0 = realBlueprint.Objects[0];
                    var dgrpField = typeof(ObjectComponent).GetField("dgrp",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var rend = dgrpField.GetValue(comp0);
                    var itemsField = rend.GetType().GetField("Items",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    foreach (var item in (System.Collections.IEnumerable)itemsField.GetValue(rend))
                    {
                        var sp = (FSO.LotView.Utils._2DStandaloneSprite)item.GetType().GetField("Sprite").GetValue(item);
                        realPix = sp.Pixel; realDepth = sp.Depth;
                        break;
                    }
                }
                catch { }
            }

            var savedPix = testSprite.Pixel;
            var savedDepth = testSprite.Depth;
            for (int variant = 0; variant < 2; variant++)
            {
                var off = 96 + variant * 96;
                if (variant == 0 && realPix != null) { testSprite.Pixel = realPix; testSprite.Depth = realDepth; }
                else { testSprite.Pixel = savedPix; testSprite.Depth = savedDepth; }
                testSprite.AbsoluteDestRect = new Rectangle(
                    st.WorldRectangle.Center.X - 32 + off, st.WorldRectangle.Center.Y - 32, 64, 64);
                testSprite.PrepareVertices(GraphicsDevice);
                _2d.PrepareImmediate(FSO.LotView.Effects.WorldBatchTechniques.drawZSprite);
                if (variant == 0)
                {
                    // Overlap theory: only re-assert viewProjection AFTER PrepareImmediate's
                    // worldViewProjection write. If registers alias, last write wins.
                    var eff2 = WorldContent._2DWorldBatchEffect;
                    var vpField = typeof(FSO.LotView.Utils._2DWorldBatch).GetField("ViewProjection",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    var batchVP = (Matrix)vpField.GetValue(_2d);
                    eff2.viewProjection = batchVP;
                }
                GraphicsDevice.DepthStencilState = DepthStencilState.None;
                _2d.DrawImmediate(testSprite);
                _2d.EndImmediate();
            }
            testSprite.Pixel = savedPix;
            testSprite.Depth = savedDepth;

            // Variant: same effect/technique, but a 3-element vertex struct that matches
            // the shader's SimpleVertex input exactly (is the 5-element _2DSpriteVertex
            // declaration what breaks KNI attribute binding?).
            {
                var eff = WorldContent._2DWorldBatchEffect;
                eff.SetTechnique(FSO.LotView.Effects.WorldBatchTechniques.drawSimple);
                eff.viewProjection = pixelVP;
                eff.pixelTexture = testSprite.Pixel;
                var r = testSprite.AbsoluteDestRect;
                r.Offset(-96, 0);
                var verts = new MiniSpriteVertex[]
                {
                    new MiniSpriteVertex(new Vector3(r.Left, r.Top, 0), new Vector2(0, 0)),
                    new MiniSpriteVertex(new Vector3(r.Right, r.Top, 0), new Vector2(1, 0)),
                    new MiniSpriteVertex(new Vector3(r.Right, r.Bottom, 0), new Vector2(1, 1)),
                    new MiniSpriteVertex(new Vector3(r.Left, r.Bottom, 0), new Vector2(0, 1)),
                };
                var inds = new short[] { 0, 1, 2, 0, 2, 3 };
                eff.CurrentTechnique.Passes[0].Apply();
                GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, verts, 0, 4, inds, 0, 2);
            }

            // Bisect (?spritetest=2): same vertex buffer, known-good BasicEffect,
            // ResetMatrices-equivalent ortho.
            if (basicEffect != null && SpriteTestBasic)
            {
                basicEffect.World = Matrix.Identity;
                basicEffect.View = pixelView;
                basicEffect.Projection = pixelProj;
                basicEffect.TextureEnabled = true;
                basicEffect.Texture = testSprite.Pixel;
                basicEffect.VertexColorEnabled = false;
                GraphicsDevice.BlendState = BlendState.AlphaBlend;
                GraphicsDevice.DepthStencilState = DepthStencilState.None;
                basicEffect.CurrentTechnique.Passes[0].Apply();
                GraphicsDevice.SetVertexBuffer(testSprite.GPUVertices);
                GraphicsDevice.DrawIndexedPrimitives(PrimitiveType.TriangleList, 0, 0, 2);
                basicEffect.TextureEnabled = false;
            }
        }

        struct MiniSpriteVertex : IVertexType
        {
            public Vector3 Position;
            public Vector2 TexCoord;
            public float ObjectID;

            public MiniSpriteVertex(Vector3 pos, Vector2 uv)
            {
                Position = pos;
                TexCoord = uv;
                ObjectID = 0;
            }

            public static readonly VertexDeclaration Declaration = new VertexDeclaration(
                new VertexElement(0, VertexElementFormat.Vector3, VertexElementUsage.Position, 0),
                new VertexElement(12, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(20, VertexElementFormat.Single, VertexElementUsage.TextureCoordinate, 1));

            VertexDeclaration IVertexType.VertexDeclaration => Declaration;
        }

        bool TryDrawRealLot()
        {
            try
            {
                realWorld.Force2DPredraw(GraphicsDevice);
                realWorld.Draw(GraphicsDevice);
                realFurniture?.Draw(GraphicsDevice, realWorld.State);
                if (SpriteTest) DrawSpriteTest();
                if (RedrawProbe && realFurniturePlaced)
                {
                    var st = realWorld.State;
                    var _2d = st._2D;
                    GraphicsDevice.DepthStencilState = DepthStencilState.None;
                    _2d.OffsetPixel(new Vector2());
                    _2d.OffsetTile(new Vector3());
                    _2d.PrepareImmediate(FSO.LotView.Effects.WorldBatchTechniques.drawZSprite);
                    var dgrpField = typeof(ObjectComponent).GetField("dgrp",
                        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                    foreach (var o in realBlueprint.Objects)
                    {
                        // Force a fresh VB upload — is the house-apply-time SetData stale?
                        try
                        {
                            var rend = dgrpField.GetValue(o);
                            var itemsField = rend.GetType().GetField("Items",
                                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                            foreach (var item in (System.Collections.IEnumerable)itemsField.GetValue(rend))
                            {
                                var sp = (FSO.LotView.Utils._2DStandaloneSprite)item.GetType().GetField("Sprite").GetValue(item);
                                // Sampler-clobber test: skip the depthTexture bind entirely.
                                sp.Depth = null;
                                sp.PrepareVertices(GraphicsDevice);
                            }
                        }
                        catch { }
                        o.Draw(GraphicsDevice, st);
                    }
                    _2d.EndImmediate();
                }
                if (!furnDrawDumped && realFurniturePlaced)
                {
                    furnDrawDumped = true;
                    var st = realWorld.State;
                    int drawable = 0;
                    foreach (var o in realBlueprint.Objects)
                        if (o.Level <= st.Level && o.DoDraw(st)) drawable++;
                    var f = realBlueprint.Objects.Count > 0 ? realBlueprint.Objects[0] : null;
                    Console.WriteLine($"furndraw drawable={drawable}/{realBlueprint.Objects.Count} " +
                        $"rect={st.WorldRectangle} bounding={f?.Bounding} order={f?.DrawOrder} level={st.Level}");
                    var cam2 = st.Camera2D;
                    var vp2 = cam2.View * cam2.Projection;
                    Console.WriteLine($"cam2dbg viewM11={cam2.View.M11:F4} viewM41={cam2.View.M41:F1},{cam2.View.M42:F1} " +
                        $"projM11={cam2.Projection.M11:F6} vpM11={vp2.M11:F6} vpM41={vp2.M41:F2},{vp2.M42:F2}");
                    try
                    {
                        var comp = realBlueprint.Objects[0];
                        var dgrpField = typeof(ObjectComponent).GetField("dgrp",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        var rend = dgrpField.GetValue(comp);
                        var itemsField = rend.GetType().GetField("Items",
                            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                            ?? rend.GetType().GetField("Items", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        var items = (System.Collections.IEnumerable)itemsField.GetValue(rend);
                        foreach (var item in items)
                        {
                            var sprite = (FSO.LotView.Utils._2DStandaloneSprite)item.GetType().GetField("Sprite").GetValue(item);
                            Console.WriteLine($"spritedbg abs={sprite.AbsoluteDestRect} src={sprite.SrcRect} dest={sprite.DestRect} " +
                                $"flip={sprite.FlipHorizontally} pixel={sprite.Pixel?.Width}x{sprite.Pixel?.Height} " +
                                $"gpuv={(sprite.GPUVertices != null)} mode={sprite.RenderMode} room={sprite.Room} floor={sprite.Floor} " +
                                $"absWorld={sprite.AbsoluteWorldPosition}");
                            if (sprite.Pixel != null)
                            {
                                var lbuf = new Color[sprite.Pixel.Width * sprite.Pixel.Height];
                                sprite.Pixel.GetData(lbuf);
                                int lvis = 0;
                                foreach (var p in lbuf) if (p.A > 0) lvis++;
                                Console.WriteLine($"livetex visiblePx={lvis}/{lbuf.Length}");
                            }
                            break;
                        }
                    }
                    catch (Exception rex) { Console.WriteLine("spritedbg failed: " + rex.Message); }
                }
                if (!realLotDebugDumped)
                {
                    realLotDebugDumped = true;
                    var st = realWorld.State;
                    Console.WriteLine($"lotdbg camMode={st.CameraMode} zoom={st.Zoom} precise={st.PreciseZoom} " +
                        $"worldPx={st.WorldSpace.WorldPx} center={st.CenterTile} level={st.Level} " +
                        $"terrainVB={(realBlueprint?.Terrain != null)} proj.M11={st.Projection.M11:F4} view.M41={st.View.M41:F1},{st.View.M42:F1}");
                }
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("real LotView draw failed → diamonds: " + Truncate(ex.Message, 140));
                if (ex.InnerException != null)
                    Console.WriteLine("  inner: " + ex.InnerException.Message);
                Console.WriteLine("  stack: " + Truncate(ex.StackTrace ?? "(none)", 1200));
                DisposeRealLot();
                realLotStatus = "real LotView draw failed → diamonds";
                return false;
            }
        }

        void DrawBasicEffectTriangle()
        {
            // Triangle uses BasicEffect. KNIF Content.Load success is the S3 status signal
            // (colorpoly2D wants View/Projection uniforms + matching VS input).
            if (basicEffect == null || effectTriangle == null) return;

            basicEffect.World = Matrix.CreateScale(0.22f)
                * Matrix.CreateTranslation(0.72f, -0.62f, 0f);
            basicEffect.View = Matrix.Identity;
            basicEffect.Projection = Matrix.CreateOrthographicOffCenter(
                -1f, 1f, -1f, 1f, -1f, 1f);

            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;

            foreach (var pass in basicEffect.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(
                    PrimitiveType.TriangleList, effectTriangle, 0, 1);
            }

            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.DepthStencilState = DepthStencilState.None;
            GraphicsDevice.RasterizerState = RasterizerState.CullCounterClockwise;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearClamp;
        }

        void DrawJoinPanel()
        {
            var vp = GraphicsDevice.Viewport;
            int panelW = Math.Min(520, vp.Width - 40);
            int panelH = 320;
            int panelX = (vp.Width - panelW) / 2;
            int panelY = (vp.Height - panelH) / 2;

            spriteBatch.Draw(pixel, new Rectangle(panelX, panelY, panelW, panelH), PanelBlue);
            spriteBatch.Draw(pixel, new Rectangle(panelX, panelY, panelW, 6), AccentBlue);

            Color texColor = sampleTexture != null ? LabelBlue
                : (loadStatus != null && loadStatus.StartsWith("texture failed", StringComparison.Ordinal) ? ErrorRed : AccentBlue);
            spriteBatch.Draw(pixel, new Rectangle(panelX + 24, panelY + 24, panelW - 48, 8), texColor);

            if (sampleTexture != null)
            {
                int texSize = 96;
                spriteBatch.Draw(sampleTexture, new Rectangle(panelX + 24, panelY + 48, texSize, texSize), Color.White);
            }

            DrawJoinStages(panelX + 140, panelY + 48, panelW - 164);

            // Effect / FreeSO-XNB status bar (S3): green = any GPU effect path; brighter
            // second segment when KNIF Content.Load succeeded.
            Color effectBar = effectOk ? OkGreen : ErrorRed;
            spriteBatch.Draw(pixel, new Rectangle(panelX + 24, panelY + panelH - 52, panelW - 48, 8), effectBar);
            if (kniEffectLoaded)
                spriteBatch.Draw(pixel, new Rectangle(panelX + 24, panelY + panelH - 52, (panelW - 48) / 2, 8), new Color(40, 255, 180));
            if (!string.IsNullOrEmpty(freeSoXnbStatus))
                spriteBatch.Draw(pixel, new Rectangle(panelX + 24, panelY + panelH - 40, panelW - 48, 6), ErrorRed);

            Color joinColor = ErrorRed;
            if (join == null) joinColor = AccentBlue;
            else if (join.Stage == JoinStage.LotJoined) joinColor = OkGreen;
            else if (join.Stage != JoinStage.Failed) joinColor = LabelBlue;
            spriteBatch.Draw(pixel, new Rectangle(panelX + 24, panelY + panelH - 28, panelW - 48, 10), joinColor);
        }

        void DrawLotPlaceholder()
        {
            var vp = GraphicsDevice.Viewport;
            float originX = vp.Width * 0.5f + lotPan.X;
            float originY = 72f + lotPan.Y;

            // Back-to-front so nearer tiles paint over farther ones
            for (int sum = 0; sum <= (LotSize - 1) * 2; sum++)
            {
                for (int x = 0; x < LotSize; x++)
                {
                    int y = sum - x;
                    if (y < 0 || y >= LotSize) continue;

                    float sx = originX + (x - y) * TileHalfW;
                    float sy = originY + (x + y) * TileHalfH;

                    bool checker = ((x + y) & 1) == 0;
                    bool house = x >= 5 && x <= 10 && y >= 5 && y <= 10;
                    Color fill = house ? HousePad : (checker ? GrassLight : GrassDark);

                    DrawDiamond((int)sx, (int)sy, TileHalfW, TileHalfH, fill);
                    if (house && (x == 5 || x == 10 || y == 5 || y == 10))
                        DrawDiamondOutline((int)sx, (int)sy, TileHalfW, TileHalfH, GrassEdge);
                }
            }
        }

        void DrawLotStatusStrip()
        {
            var vp = GraphicsDevice.Viewport;

            spriteBatch.Draw(pixel, new Rectangle(0, 0, vp.Width, 28), PanelBlue);
            // Top edge: green when effect path OK, else red
            spriteBatch.Draw(pixel, new Rectangle(0, 0, vp.Width, 4), effectOk ? OkGreen : ErrorRed);
            // Pill: solid green = BasicEffect fallback; split teal = KNIF Content.Load
            spriteBatch.Draw(pixel, new Rectangle(12, 10, 100, 8), effectOk ? OkGreen : ErrorRed);
            if (kniEffectLoaded)
                spriteBatch.Draw(pixel, new Rectangle(12, 10, 50, 8), new Color(40, 255, 180));
            // Real LotView indicator: lime when drawing ExternalWorld, amber when failed/fallback
            if (_forceRealLot)
            {
                var realColor = realLotReady ? new Color(180, 255, 80) : new Color(255, 180, 60);
                spriteBatch.Draw(pixel, new Rectangle(120, 10, 40, 8), realColor);
            }
            if (!string.IsNullOrEmpty(freeSoXnbStatus))
                spriteBatch.Draw(pixel, new Rectangle(168, 10, 60, 8), ErrorRed);
            if (sampleTexture != null)
                spriteBatch.Draw(sampleTexture, new Rectangle(vp.Width - 40, 4, 20, 20), Color.White);
            // Join stage chip: gray = not started, amber = in progress, green = LotJoined.
            if (joinStarted && join != null)
            {
                var joinColor = join.Stage == JoinStage.LotJoined ? OkGreen
                    : new Color(255, 180, 60);
                spriteBatch.Draw(pixel, new Rectangle(236, 10, 40, 8), joinColor);
                if (join.Stage != lastLoggedJoinStage)
                {
                    lastLoggedJoinStage = join.Stage;
                    Console.WriteLine($"joinstage {join.Stage}");
                }
            }
        }

        void DrawDiamond(int cx, int cy, int halfW, int halfH, Color color)
        {
            for (int dy = -halfH; dy <= halfH; dy++)
            {
                float t = 1f - Math.Abs(dy) / (float)Math.Max(1, halfH);
                int halfSpan = Math.Max(1, (int)(halfW * t));
                spriteBatch.Draw(pixel, new Rectangle(cx - halfSpan, cy + dy, halfSpan * 2, 1), color);
            }
        }

        void DrawDiamondOutline(int cx, int cy, int halfW, int halfH, Color color)
        {
            spriteBatch.Draw(pixel, new Rectangle(cx - 1, cy - halfH, 2, 1), color);
            spriteBatch.Draw(pixel, new Rectangle(cx - 1, cy + halfH, 2, 1), color);
            spriteBatch.Draw(pixel, new Rectangle(cx - halfW, cy - 1, 1, 2), color);
            spriteBatch.Draw(pixel, new Rectangle(cx + halfW - 1, cy - 1, 1, 2), color);
        }

        void DrawJoinStages(int x, int y, int width)
        {
            var stages = new[]
            {
                JoinStage.CityConnecting, JoinStage.CityHandshake, JoinStage.CitySessionSent,
                JoinStage.CityHostOnline, JoinStage.CityClientOnline, JoinStage.AvatarSelect,
                JoinStage.FindLot, JoinStage.LotConnecting, JoinStage.LotSession,
                JoinStage.LotHostOnline, JoinStage.LotJoined,
            };
            int gap = 4;
            int h = 10;
            int n = stages.Length;
            int w = Math.Max(4, (width - gap * (n - 1)) / n);
            var current = join?.Stage ?? JoinStage.Idle;

            for (int i = 0; i < n; i++)
            {
                Color c = new Color(40, 44, 64);
                if (join != null)
                {
                    if (join.Stage == JoinStage.Failed && stages[i] == JoinStage.LotJoined)
                        c = ErrorRed;
                    else if ((int)current >= (int)stages[i] && current != JoinStage.Failed)
                        c = stages[i] == JoinStage.LotJoined && current == JoinStage.LotJoined ? OkGreen : AccentBlue;
                }
                spriteBatch.Draw(pixel, new Rectangle(x + i * (w + gap), y, w, h), c);
            }

            float t = 0;
            if (join != null && join.Stage != JoinStage.Failed && join.Stage != JoinStage.Idle)
                t = Math.Min(1f, (int)join.Stage / (float)JoinStage.LotJoined);
            int fill = (int)(width * t);
            spriteBatch.Draw(pixel, new Rectangle(x, y + 24, width, 8), new Color(40, 44, 64));
            if (fill > 0)
                spriteBatch.Draw(pixel, new Rectangle(x, y + 24, fill, 8),
                    join?.Stage == JoinStage.LotJoined ? OkGreen : LabelBlue);
        }

        static string Truncate(string s, int max)
        {
            if (string.IsNullOrEmpty(s) || s.Length <= max) return s;
            return s.Substring(0, max - 1) + "…";
        }
    }
}
