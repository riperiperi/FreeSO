using FSO.Content.Codecs;
using FSO.Content.Framework;
using FSO.Vitaboy;
using System.Text.RegularExpressions;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to outfit (*.oft) data in FAR3 archives.
    /// </summary>
    public class AvatarOutfitProvider : TSOAvatarContentProvider<Outfit>
    {
        public AvatarOutfitProvider(Content contentManager) : base(contentManager, new OutfitCodec(),
            new Regex(".*/outfits/.*\\.dat"),
            new Regex("Avatar/Outfits/.*\\.oft"))
        {
        }
    }
}
