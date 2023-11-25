using FSO.Content.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Content.Codecs;
using System.Text.RegularExpressions;
using FSO.Vitaboy;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to mesh (*.mesh) data in FAR3 archives.
    /// </summary>
    public class AvatarMeshProvider : TSOAvatarContentProvider<Mesh>
    {
        public AvatarMeshProvider(Content contentManager, GraphicsDevice device) : base(contentManager, new MeshCodec(),
            new Regex(".*/meshes/.*\\.dat"),
            new Regex("Avatar/Meshes/.*\\.mesh"))
        {
        }
    }
}
