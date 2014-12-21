/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the SimsLib.

The Initial Developer of the Original Code is
Mats "Afr0" Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SimsLib.ThreeD
{
    /// <summary>
    /// Represents an outfit that can be purchased by a Sim.
    /// </summary>
    public class PurchasableOutfit
    {
        private uint m_Version;
        private uint m_Gender;          //0 if male, 1 if female.
        private uint m_AssetIDSize;     //Should be 8.
        private ulong m_OutfitAssetID;

        public ulong OutfitID
        {
            get { return m_OutfitAssetID; }
        }

        /// <summary>
        /// Creates a new purchasable outfit.
        /// </summary>
        /// <param name="Str">The stream used to create the purchasable outfit from.</param>
        public PurchasableOutfit(Stream Str)
        {
            using (IoBuffer Reader = new IoBuffer(Str))
            {
                Reader.ByteOrder = ByteOrder.BIG_ENDIAN;

                m_Version = Reader.ReadUInt32();
                m_Gender = Reader.ReadUInt32();
                m_AssetIDSize = Reader.ReadUInt32();

                Reader.ReadUInt32(); //AssetID prefix... typical useless Maxis value.

                m_OutfitAssetID = Reader.ReadUInt64();
            }
        }
    }
}
