using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content.framework;
using System.Text.RegularExpressions;
using tso.vitaboy;
using tso.content.codecs;

namespace tso.content
{
    public class AvatarPurchasables : FAR3Provider<PurchasableOutfit>
    {
        public AvatarPurchasables(Content contentManager)
            : base(contentManager, new PurchasableOutfitCodec(), new Regex(".*\\\\purchasables\\\\.*\\.dat"))
        {
        }
    }
}
