using System;
using System.Threading.Tasks;
using FSO.BrowserContent;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FSO_BrowserClient
{
    /// <summary>
    /// KNI/BlazorGL spike: clear color + one texture loaded through <see cref="HttpContentStore"/>.
    /// </summary>
    public class FSO_BrowserClientGame : Game
    {
        static readonly Color ClearBlue = new Color(15, 18, 32);      // #0f1220
        static readonly Color AccentBlue = new Color(79, 110, 247);   // #4f6ef7
        static readonly Color PanelBlue = new Color(24, 28, 48);      // #181c30
        static readonly Color LabelBlue = new Color(140, 170, 255);
        static readonly Color ErrorRed = new Color(220, 80, 90);

        readonly string _contentBaseUrl;

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D pixel;
        Texture2D sampleTexture;
        string loadStatus = "loading…";
        bool loadStarted;

        /// <param name="contentBaseUrl">Absolute URL of the sample-content root (trailing slash OK).</param>
        public FSO_BrowserClientGame(string contentBaseUrl)
        {
            _contentBaseUrl = contentBaseUrl ?? throw new ArgumentNullException(nameof(contentBaseUrl));
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Window.Title = "FreeSO Browser";
        }

        protected override void Initialize()
        {
            base.Initialize();
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
                // Relative to sample-content/ — proves IContentStore path used by Content.GetResource.
                using (var stream = await store.OpenAsync("textures/squares.png").ConfigureAwait(true))
                {
                    sampleTexture = Texture2D.FromStream(GraphicsDevice, stream);
                }
                loadStatus = "HttpContentStore → Texture2D OK";
            }
            catch (Exception ex)
            {
                loadStatus = "load failed: " + ex.GetType().Name + ": " + ex.Message;
                System.Diagnostics.Debug.WriteLine(loadStatus);
                Console.WriteLine(loadStatus);
            }
        }

        protected override void UnloadContent()
        {
            sampleTexture?.Dispose();
            sampleTexture = null;
            pixel?.Dispose();
            pixel = null;
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboardState = Keyboard.GetState();
            GamePadState gamePadState = GamePad.GetState(PlayerIndex.One);

            if (keyboardState.IsKeyDown(Keys.Escape) ||
                keyboardState.IsKeyDown(Keys.Back) ||
                gamePadState.Buttons.Back == ButtonState.Pressed)
            {
                try { Exit(); }
                catch (PlatformNotSupportedException) { /* ignore */ }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(ClearBlue);

            var vp = GraphicsDevice.Viewport;
            int panelW = Math.Min(480, vp.Width - 40);
            int panelH = sampleTexture != null ? 280 : 140;
            int panelX = (vp.Width - panelW) / 2;
            int panelY = (vp.Height - panelH) / 2;

            spriteBatch.Begin();
            spriteBatch.Draw(pixel, new Rectangle(panelX, panelY, panelW, panelH), PanelBlue);
            spriteBatch.Draw(pixel, new Rectangle(panelX, panelY, panelW, 6), AccentBlue);

            // Status bar (stand-in for fonts): green-ish when OK, red-ish on error, blue while loading.
            Color statusColor = sampleTexture != null ? LabelBlue
                : (loadStatus != null && loadStatus.StartsWith("load failed", StringComparison.Ordinal) ? ErrorRed : AccentBlue);
            spriteBatch.Draw(pixel, new Rectangle(panelX + 24, panelY + 24, panelW - 48, 10), statusColor);

            if (sampleTexture != null)
            {
                int texSize = Math.Min(180, panelW - 48);
                int texX = panelX + (panelW - texSize) / 2;
                int texY = panelY + 56;
                spriteBatch.Draw(sampleTexture, new Rectangle(texX, texY, texSize, texSize), Color.White);
                spriteBatch.Draw(pixel, new Rectangle(texX, texY + texSize + 16, texSize, 8), AccentBlue);
            }
            else
            {
                int barY = panelY + 56;
                spriteBatch.Draw(pixel, new Rectangle(panelX + 40, barY, panelW - 80, 14), LabelBlue);
                spriteBatch.Draw(pixel, new Rectangle(panelX + 40, barY + 28, (panelW - 80) * 2 / 3, 10), statusColor);
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
