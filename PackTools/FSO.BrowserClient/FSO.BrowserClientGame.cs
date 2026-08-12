using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FSO_BrowserClient
{
    /// <summary>
    /// Minimal KNI/BlazorGL spike for Phase F — FreeSO branding only, not a client port.
    /// </summary>
    public class FSO_BrowserClientGame : Game
    {
        static readonly Color ClearBlue = new Color(15, 18, 32);      // #0f1220
        static readonly Color AccentBlue = new Color(79, 110, 247);   // #4f6ef7
        static readonly Color PanelBlue = new Color(24, 28, 48);      // #181c30
        static readonly Color LabelBlue = new Color(140, 170, 255);

        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D pixel;

        public FSO_BrowserClientGame()
        {
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
        }

        protected override void UnloadContent()
        {
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
            int panelW = Math.Min(420, vp.Width - 40);
            int panelH = 120;
            int panelX = (vp.Width - panelW) / 2;
            int panelY = (vp.Height - panelH) / 2;

            spriteBatch.Begin();
            // Centered panel — stands in for a "FreeSO Browser" title until fonts ship.
            spriteBatch.Draw(pixel, new Rectangle(panelX, panelY, panelW, panelH), PanelBlue);
            spriteBatch.Draw(pixel, new Rectangle(panelX, panelY, panelW, 6), AccentBlue);
            // Three accent bars: rough stand-in for the words "FreeSO Browser".
            int barY = panelY + 40;
            spriteBatch.Draw(pixel, new Rectangle(panelX + 40, barY, panelW - 80, 14), LabelBlue);
            spriteBatch.Draw(pixel, new Rectangle(panelX + 40, barY + 28, (panelW - 80) * 2 / 3, 10), AccentBlue);
            spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
