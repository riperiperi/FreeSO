using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace tso.files.utils
{
    public interface IWorldTextureProvider
    {
        WorldTexture GetWorldTexture(GraphicsDevice device);
    }
}
