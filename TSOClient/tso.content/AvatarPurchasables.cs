using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content.framework;
using System.Text.RegularExpressions;
using TSO.Vitaboy;
using TSO.Content.codecs;

namespace TSO.Content
{
    /// <summary>
    /// Provides access to purchasable (*.po) data in FAR3 archives.
    /// </summary>
    public class AvatarPurchasables : FAR3Provider<PurchasableOutfit>
    {
        public AvatarPurchasables(Content contentManager)
            : base(contentManager, new PurchasableOutfitCodec(), new Regex(".*\\\\purchasables\\\\.*\\.dat"))
        {
        }
    }
}
