using MGWinForms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace FSO.IDE.EditorComponent
{
    public class BHAVViewComponent : GameControl
    {
        private BHAVViewEngine _engine;
        protected override void Initialize()
        {
            base.Initialize();

            _engine = new BHAVViewEngine(GraphicsDeviceService, Services);
            _engine.Initialize();
        }

        protected override void Draw(GameTime gameTime)
        {
            _engine.Draw(gameTime);
        }

        protected override void Update(GameTime gameTime)
        {
            _engine.Update(gameTime);
        }
    }
}
