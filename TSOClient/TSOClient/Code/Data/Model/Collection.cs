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
using SimsLib.ThreeD;

namespace TSOClient.Code.Data.Model
{
    public class Collection : List<CollectionItem>
    {
        public Collection(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                Parse(new BinaryReader(stream));
            }
        }

        public Collection(BinaryReader reader)
        {
            Parse(reader);
        }

        private void Parse(BinaryReader reader)
        {
            var count = Endian.SwapInt32(reader.ReadInt32());

            for (int i = 0; i < count; i++)
            {
                var item = new CollectionItem
                {
                    Index = reader.ReadInt32(),
                    FileID = Endian.SwapUInt64(reader.ReadUInt64())
                };

                this.Add(item);
            }
        }
    }

    public class CollectionItem
    {
        public int Index;
        public ulong FileID;

        private PurchasableOutfit m_PurchasableOutfit;
        public PurchasableOutfit PurchasableOutfit
        {
            get
            {
                if (m_PurchasableOutfit == null)
                {
                    m_PurchasableOutfit = new PurchasableOutfit(new MemoryStream(ContentManager.GetResourceFromLongID(FileID)));
                }
                return m_PurchasableOutfit;
            }
        }
    }
}