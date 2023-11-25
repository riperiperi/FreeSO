using FSO.Content.Framework;
using FSO.Content.Codecs;
using System.Text.RegularExpressions;
using FSO.Content.Model;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to avatar thumbnail data in FAR3 archives.
    /// </summary>
    public class AvatarThumbnailProvider : TSOAvatarContentProvider<ITextureRef>
    {
        public AvatarThumbnailProvider(Content contentManager) : base(contentManager, new TextureCodec(),
            new Regex(".*/thumbnails/.*\\.dat"),
            new Regex("Avatar/Thumbnails/.*"))
        {
        }
    }
}
