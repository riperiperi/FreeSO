using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace tso.world.components
{
    public class DebugSprite : WorldComponent
    {

        public override float PreferredDrawOrder
        {
            get { return 0; }
        }
        public override void Draw(GraphicsDevice device, WorldState world){

        }
    }
}
