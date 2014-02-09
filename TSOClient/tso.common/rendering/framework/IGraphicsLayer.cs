using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.common.rendering.framework.model;
using Microsoft.Xna.Framework.Graphics;

namespace tso.common.rendering.framework
{
    public interface IGraphicsLayer
    {
        void Initialize(GraphicsDevice device);
        void Update(UpdateState state);
        void PreDraw(GraphicsDevice device);
        void Draw(GraphicsDevice device);
    }
}
