using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace SimsLib
{
    public interface ITextureProvider
    {
        Texture2D GetTexture(GraphicsDevice device);
    }
}