using FSO.Content.Framework;
using System.Text.RegularExpressions;
using FSO.Vitaboy;
using FSO.Content.Codecs;

namespace FSO.Content
{
    /// <summary>
    /// Provides access to purchasable (*.po) data in FAR3 archives.
    /// </summary>
    public class AvatarPurchasables : TSOAvatarContentProvider<PurchasableOutfit>
    {
        public AvatarPurchasables(Content contentManager) : base(contentManager, new PurchasableOutfitCodec(),
            new Regex(".*/purchasables/.*\\.dat"),
            new Regex("Avatar/Purchasables/.*\\.po"))
        {
        }
    }
}
