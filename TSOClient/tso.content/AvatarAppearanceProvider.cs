using FSO.Content.Framework;
using FSO.Vitaboy;
using FSO.Content.Codecs;
using System.Text.RegularExpressions;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to appearance (*.apr) data in FAR3 archives.
    /// </summary>
    public class AvatarAppearanceProvider : TSOAvatarContentProvider<Appearance>
    {
        public AvatarAppearanceProvider(Content contentManager) : base(contentManager, new AppearanceCodec(),
            new Regex(".*/appearances/.*\\.dat"),
            new Regex("Avatar/Appearances/.*\\.apr"))
        {
        }
    }
}
