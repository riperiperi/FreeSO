using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using TSO.Files.utils;

namespace TSO.Vitaboy
{
    public class Collection : List<CollectionItem>
    {
        public Collection(){
        }

        public Collection(byte[] data)
        {
            using (var stream = new MemoryStream(data))
            {
                this.Read(stream);
            }
        }

        public void Read(Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.BIG_ENDIAN)){
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
