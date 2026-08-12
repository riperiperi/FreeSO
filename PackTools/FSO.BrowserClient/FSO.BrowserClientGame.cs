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
    /// KNI/BlazorGL spike: HTTP texture + optional Aries city→lot join via gateway.
    /// </summary>
    public class FSO_BrowserClientGame : Game
    {
        static readonly Color ClearBlue = new Color(15, 18, 32);
        static readonly Color AccentBlue = new Color(79, 110, 247);
        static readonly Color PanelBlue = new Color(24, 28, 48);
        static readonly Color LabelBlue = new Color(140, 170, 255);
        static readonly Color ErrorRed = new Color(220, 80, 90);
        static readonly Color OkGreen = new Color(62, 207, 142);

        readonly string _contentBaseUrl;
        readonly string _gatewayBase;

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

        /// <param name="contentBaseUrl">Absolute URL of sample-content root.</param>
        /// <param name="gatewayBase">Gateway base (http://127.0.0.1:8087 or ws://…).</param>
        public FSO_BrowserClientGame(string contentBaseUrl, string gatewayBase)
        {
            _contentBaseUrl = contentBaseUrl ?? throw new ArgumentNullException(nameof(contentBaseUrl));
            _gatewayBase = gatewayBase ?? throw new ArgumentNullException(nameof(gatewayBase));
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Window.Title = "FreeSO Browser";
        }

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
                loadStatus = "texture OK — press Space to join via gateway";
            }
            catch (Exception ex)
            {
                loadStatus = "texture failed: " + ex.GetType().Name + ": " + ex.Message;
                Console.WriteLine(loadStatus);
            }
        }

        void StartJoin()
        {
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

            // Auto-join shortly after texture load so CI/smoke doesn't need keyboard.
            if (!joinStarted && sampleTexture != null && gameTime.TotalGameTime.TotalSeconds > 1.5)
                StartJoin();

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(ClearBlue);

            var vp = GraphicsDevice.Viewport;
            int panelW = Math.Min(520, vp.Width - 40);
            int panelH = 320;
            int panelX = (vp.Width - panelW) / 2;
            int panelY = (vp.Height - panelH) / 2;

            spriteBatch.Begin();
            spriteBatch.Draw(pixel, new Rectangle(panelX, panelY, panelW, panelH), PanelBlue);
            spriteBatch.Draw(pixel, new Rectangle(panelX, panelY, panelW, 6), AccentBlue);

            // Texture status bar
            Color texColor = sampleTexture != null ? LabelBlue
                : (loadStatus != null && loadStatus.StartsWith("texture failed", StringComparison.Ordinal) ? ErrorRed : AccentBlue);
            spriteBatch.Draw(pixel, new Rectangle(panelX + 24, panelY + 24, panelW - 48, 8), texColor);

            if (sampleTexture != null)
            {
                int texSize = 96;
                spriteBatch.Draw(sampleTexture, new Rectangle(panelX + 24, panelY + 48, texSize, texSize), Color.White);
            }

            // Join stage bars (12 slots)
            DrawJoinStages(panelX + 140, panelY + 48, panelW - 164);

            // Bottom status strip color
            Color joinColor = ErrorRed;
            if (join == null) joinColor = AccentBlue;
            else if (join.Stage == JoinStage.LotJoined) joinColor = OkGreen;
            else if (join.Stage != JoinStage.Failed) joinColor = LabelBlue;
            spriteBatch.Draw(pixel, new Rectangle(panelX + 24, panelY + panelH - 28, panelW - 48, 10), joinColor);

            spriteBatch.End();
            base.Draw(gameTime);
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

            // Second row: echo progress as wider bar fill
            float t = 0;
            if (join != null && join.Stage != JoinStage.Failed && join.Stage != JoinStage.Idle)
                t = Math.Min(1f, (int)join.Stage / (float)JoinStage.LotJoined);
            int fill = (int)((width) * t);
            spriteBatch.Draw(pixel, new Rectangle(x, y + 24, width, 8), new Color(40, 44, 64));
            if (fill > 0)
                spriteBatch.Draw(pixel, new Rectangle(x, y + 24, fill, 8),
                    join?.Stage == JoinStage.LotJoined ? OkGreen : LabelBlue);
        }
    }
}
