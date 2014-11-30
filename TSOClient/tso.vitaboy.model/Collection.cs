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
using TSO.Files.utils;

namespace TSO.Vitaboy
{
    /// <summary>
    /// Collections provide a packaged list of purchasable outfits.
    /// </summary>
    public class Collection : List<CollectionItem>
    {
        /// <summary>
        /// Creates a new Collection instance.
        /// </summary>
        public Collection()
        {
        }

        /// <summary>
        /// Creates a new Collection instance from a stream of bytes.
        /// </summary>
        /// <param name="data">A stream of bytes with collection data.</param>
        public Collection(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                this.Read(stream);
            }
        }

        /// <summary>
        /// Reads a collection from a stream.
        /// </summary>
        /// <param name="stream">A Stream instance holding a collection.</param>
        public void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.BIG_ENDIAN))
            {
                var count = io.ReadInt32();
                for (int i = 0; i < count; i++){
                    var item = new CollectionItem
                    {
                        Index = io.ReadInt32(),
                        FileID = io.ReadUInt32(),
                        TypeID = io.ReadUInt32()
                    };
                    this.Add(item);
                }
            }
        }
    }

    /// <summary>
    /// An item in a collection, pointing to a purchasable outfit.
    /// </summary>
    public class CollectionItem
    {
        public int Index;
        public uint FileID;
        public uint TypeID;

        public ulong PurchasableOutfitId
        {
            get
            {
                MemoryStream MemStream = new MemoryStream();
                BinaryWriter Writer = new BinaryWriter(MemStream);

                Writer.Write(TypeID);
                Writer.Write(FileID);

                return BitConverter.ToUInt64(MemStream.ToArray(), 0);
            }
        }
    }
}
