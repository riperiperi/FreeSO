/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Content.Framework;
using FSO.Vitaboy;

namespace FSO.Content.Codecs
{
    /// <summary>
    /// Codec for purchasable outfits (*.po).
    /// </summary>
    public class PurchasableOutfitCodec : IContentCodec<PurchasableOutfit>
    {
        #region IContentCodec<Outfit> Members

        public override object GenDecode(System.IO.Stream stream)
        {
            var outfit = new PurchasableOutfit();
            outfit.Read(stream);
            return outfit;
        }

        #endregion
    }
}
