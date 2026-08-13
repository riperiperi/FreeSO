using System;
using System.Threading;
using System.Threading.Tasks;
using FSO.BrowserAries;
using FSO.BrowserContent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FSO_BrowserClient
{
    /// <summary>
    /// KNI/BlazorGL spike: HTTP texture + Aries city→lot join + isometric lot placeholder.
    /// S3: prefer <c>Content.Load&lt;Effect&gt;("Effects/colorpoly2D")</c> (KNIF from
    /// FSO.BrowserEffects); fall back to <see cref="BasicEffect"/>. Stock FreeSO MGFX 11
    /// probe via <c>?effect=1</c> (sample-content). Real LotView still blocked.
    /// </summary>
    public class FSO_BrowserClientGame : Game
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

        readonly string _contentBaseUrl;
        readonly string _gatewayBase;
        readonly bool _autoJoin;
        readonly bool _forceLotView;
        readonly bool _probeFreeSoXnb;

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
        bool effectOk;
        bool kniEffectLoaded;
        bool loadStarted;
        bool joinStarted;
        bool spaceWasDown;

        ArchiveJoinDemo join;
        CancellationTokenSource joinCts;

        Vector2 lotPan;

        /// <param name="contentBaseUrl">Absolute URL of sample-content root.</param>
        /// <param name="gatewayBase">Gateway base (http://127.0.0.1:8087 or ws://…).</param>
        /// <param name="autoJoin">When true, start city→lot join ~1.5s after texture load.</param>
        /// <param name="forceLotView">When true (<c>?lot=1</c>), show isometric placeholder without joining.</param>
        /// <param name="probeFreeSoXnb">When true (<c>?effect=1</c>), Content.Load stock FreeSO colorpoly2D from sample-content.</param>
        public FSO_BrowserClientGame(
            string contentBaseUrl,
            string gatewayBase,
            bool autoJoin = false,
            bool forceLotView = false,
            bool probeFreeSoXnb = false)
        {
            _contentBaseUrl = contentBaseUrl ?? throw new ArgumentNullException(nameof(contentBaseUrl));
            _gatewayBase = gatewayBase ?? throw new ArgumentNullException(nameof(gatewayBase));
            _autoJoin = autoJoin;
            _forceLotView = forceLotView;
            _probeFreeSoXnb = probeFreeSoXnb;
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Window.Title = "FreeSO Browser";
        }

        bool ShowLotFloor =>
            _forceLotView || (join != null && join.Stage == JoinStage.LotJoined);

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

            if (!loadStarted)
            {
                loadStarted = true;
                _ = LoadSampleViaHttpStoreAsync();
            }
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

        async Task LoadSampleViaHttpStoreAsync()
        {
            try
            {
                using var store = new HttpContentStore(_contentBaseUrl);
                using (var stream = await store.OpenAsync("textures/squares.png").ConfigureAwait(true))
                {
                    sampleTexture = Texture2D.FromStream(GraphicsDevice, stream);
                }
                if (_forceLotView)
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
            if (joinStarted || _forceLotView) return;
            joinStarted = true;
            joinCts = new CancellationTokenSource();
            join = new ArchiveJoinDemo(_gatewayBase);
            join.Changed += () => { /* status read each Draw */ };
            _ = join.RunAsync(joinCts.Token);
        }

        protected override void UnloadContent()
        {
            joinCts?.Cancel();
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

            if (_autoJoin && !joinStarted && sampleTexture != null
                && gameTime.TotalGameTime.TotalSeconds > 1.5)
                StartJoin();

            if (ShowLotFloor)
            {
                const float panSpeed = 120f;
                float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
                if (keyboardState.IsKeyDown(Keys.Left) || keyboardState.IsKeyDown(Keys.A))
                    lotPan.X += panSpeed * dt;
                if (keyboardState.IsKeyDown(Keys.Right) || keyboardState.IsKeyDown(Keys.D))
                    lotPan.X -= panSpeed * dt;
                if (keyboardState.IsKeyDown(Keys.Up) || keyboardState.IsKeyDown(Keys.W))
                    lotPan.Y += panSpeed * dt;
                if (keyboardState.IsKeyDown(Keys.Down) || keyboardState.IsKeyDown(Keys.S))
                    lotPan.Y -= panSpeed * dt;
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(ShowLotFloor ? new Color(20, 28, 18) : ClearBlue);

            if (ShowLotFloor)
            {
                spriteBatch.Begin();
                DrawLotPlaceholder();
                spriteBatch.End();

                DrawBasicEffectTriangle();

                spriteBatch.Begin();
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
            if (!string.IsNullOrEmpty(freeSoXnbStatus))
                spriteBatch.Draw(pixel, new Rectangle(120, 10, 60, 8), ErrorRed);
            if (sampleTexture != null)
                spriteBatch.Draw(sampleTexture, new Rectangle(vp.Width - 40, 4, 20, 20), Color.White);
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
