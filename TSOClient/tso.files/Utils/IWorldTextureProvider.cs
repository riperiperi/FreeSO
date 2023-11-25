using Microsoft.Xna.Framework.Graphics;

namespace FSO.Files.Utils
{
    public interface IWorldTextureProvider
    {
        WorldTexture GetWorldTexture(GraphicsDevice device);
    }
}
