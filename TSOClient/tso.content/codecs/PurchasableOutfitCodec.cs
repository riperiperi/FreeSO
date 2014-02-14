using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.content.framework;
using tso.vitaboy;

namespace tso.content.codecs
{
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
