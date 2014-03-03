using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSO.Common.rendering.framework;

namespace tso.world.utils
{
    public class _3DSprite {
        public _3DSpriteEffect Effect;
        public Matrix World;
        public Texture2D Texture;
        public I3DGeometry Geometry;
    }

    public enum _3DSpriteEffect {
        CHARACTER
    }
}
