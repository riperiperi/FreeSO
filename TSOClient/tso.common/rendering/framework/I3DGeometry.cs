using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace tso.common.rendering.framework
{
    public interface I3DGeometry {
        void DrawGeometry(GraphicsDevice gd);
    }
}
