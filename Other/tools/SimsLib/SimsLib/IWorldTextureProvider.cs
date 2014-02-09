using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace SimsLib
{
    public interface IWorldTextureProvider
    {
        WorldTexture GetWorldTexture(GraphicsDevice device);
    }
}