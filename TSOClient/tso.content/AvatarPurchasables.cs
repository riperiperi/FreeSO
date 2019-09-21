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
