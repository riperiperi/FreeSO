using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace tso.common.rendering.framework
{
    public abstract class Game : Microsoft.Xna.Framework.Game
    {
        protected GraphicsDeviceManager Graphics;
        protected GameScreen Screen;

        public Game()
        {
            Graphics = new GraphicsDeviceManager(this);
        }

        protected override void Initialize(){
            base.Initialize();

            Screen = new GameScreen(GraphicsDevice);
        }

        protected override void Update(GameTime gameTime){
            Screen.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime){
            base.Draw(gameTime);
            Screen.Draw(gameTime);
        }
    }
}
