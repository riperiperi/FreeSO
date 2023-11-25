using FSO.Content.Framework;
using FSO.Content.Codecs;
using System.Text.RegularExpressions;
using FSO.Content.Model;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to texture (*.jpg) data in FAR3 archives.
    /// </summary>
    public class AvatarTextureProvider : TSOAvatarContentProvider<ITextureRef>
    {
        public AvatarTextureProvider(Content contentManager) : base(contentManager, new TextureCodec(),
            new Regex(".*/textures/.*\\.dat"),
            new Regex("Avatar/Textures/.*"))
        {
        }
    }
}
