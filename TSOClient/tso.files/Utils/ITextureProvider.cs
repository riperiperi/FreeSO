using Microsoft.Xna.Framework.Graphics;

namespace FSO.Files.Utils
{
    public interface ITextureProvider
    {
        Texture2D GetTexture(GraphicsDevice device);
    }
}
