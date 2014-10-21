/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using SimsLib;

namespace TSO.Vitaboy
{
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
        /// <param name="FileData">The data to create the purchasable outfit from.</param>
        public PurchasableOutfit()
        {
        }

        public void Read(Stream stream)
        {
            BinaryReader Reader = new BinaryReader(stream);
            m_Version = Endian.SwapUInt32(Reader.ReadUInt32());
            m_Gender = Endian.SwapUInt32(Reader.ReadUInt32());
            m_AssetIDSize = Endian.SwapUInt32(Reader.ReadUInt32());
            Reader.ReadUInt32(); //AssetID prefix... typical useless Maxis value.
            m_OutfitAssetID = Endian.SwapUInt64(Reader.ReadUInt64());
            Reader.Close();
        }
    }
}
