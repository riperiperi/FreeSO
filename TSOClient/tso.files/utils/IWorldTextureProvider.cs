using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace TSO.Files.utils
{
    public interface IWorldTextureProvider
    {
        WorldTexture GetWorldTexture(GraphicsDevice device);
    }
}
