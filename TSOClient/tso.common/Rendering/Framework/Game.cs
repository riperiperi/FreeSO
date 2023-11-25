using Microsoft.Xna.Framework;

namespace FSO.Common.Rendering.Framework
{
    public abstract class Game : Microsoft.Xna.Framework.Game
    {
        protected GraphicsDeviceManager Graphics;
        protected GameScreen Screen;

		public Game() : base()
        {
            Graphics = new GraphicsDeviceManager(this);
        }

        protected override void Initialize(){
            base.Initialize();

            Screen = new GameScreen(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime){
            Screen.Update(gameTime, IsActive);
        }

        protected override void Draw(GameTime gameTime){
            base.Draw(gameTime);
            Screen.Draw(gameTime);
        }
    }
}
