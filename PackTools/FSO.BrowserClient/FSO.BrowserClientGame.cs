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
    /// Real FSO.LotView is blocked (Mario.dll / S3 effects / TFM); this draws a grass diamond
    /// grid after LotJoined (or with <c>?lot=1</c>) as the S5 visual stand-in.
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

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D pixel;
        Texture2D sampleTexture;
        string loadStatus = "loading…";
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
        public FSO_BrowserClientGame(
            string contentBaseUrl,
            string gatewayBase,
            bool autoJoin = false,
            bool forceLotView = false)
        {
            _contentBaseUrl = contentBaseUrl ?? throw new ArgumentNullException(nameof(contentBaseUrl));
            _gatewayBase = gatewayBase ?? throw new ArgumentNullException(nameof(gatewayBase));
            _autoJoin = autoJoin;
            _forceLotView = forceLotView;
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

            if (!loadStarted)
            {
                loadStarted = true;
                _ = LoadSampleViaHttpStoreAsync();
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
                    loadStatus = "texture OK — lot placeholder (?lot=1)";
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

            spriteBatch.Begin();
            if (ShowLotFloor)
                DrawLotPlaceholder();
            else
                DrawJoinPanel();
            spriteBatch.End();
            base.Draw(gameTime);
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

            Color joinColor = ErrorRed;
            if (join == null) joinColor = AccentBlue;
            else if (join.Stage == JoinStage.LotJoined) joinColor = OkGreen;
            else if (join.Stage != JoinStage.Failed) joinColor = LabelBlue;
            spriteBatch.Draw(pixel, new Rectangle(panelX + 24, panelY + panelH - 28, panelW - 48, 10), joinColor);
        }

        void DrawLotPlaceholder()
        {
            var vp = GraphicsDevice.Viewport;

            // Status strip
            spriteBatch.Draw(pixel, new Rectangle(0, 0, vp.Width, 28), PanelBlue);
            spriteBatch.Draw(pixel, new Rectangle(0, 0, vp.Width, 4), OkGreen);
            spriteBatch.Draw(pixel, new Rectangle(12, 10, 80, 8), OkGreen);
            if (sampleTexture != null)
                spriteBatch.Draw(sampleTexture, new Rectangle(vp.Width - 40, 4, 20, 20), Color.White);

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
                    // thin edge on house pad
                    if (house && (x == 5 || x == 10 || y == 5 || y == 10))
                        DrawDiamondOutline((int)sx, (int)sy, TileHalfW, TileHalfH, GrassEdge);
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
            // four edges as 1px diamonds inset — keep cheap: top/bottom tips + mid ring
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
    }
}
