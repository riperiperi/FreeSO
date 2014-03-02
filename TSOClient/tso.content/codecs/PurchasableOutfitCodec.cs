using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content.framework;
using tso.vitaboy;

namespace tso.content.codecs
{
    /// <summary>
    /// Codec for purchasable outfits (*.po).
    /// </summary>
    public class PurchasableOutfitCodec : IContentCodec<PurchasableOutfit>
    {
        #region IContentCodec<Outfit> Members

        public PurchasableOutfit Decode(System.IO.Stream stream)
        {
            var outfit = new PurchasableOutfit();
            outfit.Read(stream);
            return outfit;
        }

        #endregion
    }
}
