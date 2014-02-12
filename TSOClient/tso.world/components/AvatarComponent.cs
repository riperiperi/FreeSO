using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using tso.vitaboy;

namespace tso.world.components
{
    public class AvatarComponent : WorldComponent
    {
        public Avatar Avatar;

        public override float PreferredDrawOrder
        {
            get { return 5000.0f;  }
        }

        public override void Initialize(GraphicsDevice device, WorldState world)
        {
            base.Initialize(device, world);
            Avatar.StoreOnGPU(device);
        }

        public override void Draw(GraphicsDevice device, WorldState world)
        {
            if (Avatar != null){
                world._3D.DrawMesh(this.World, Avatar.Bindings);
            }
        }
    }
}
