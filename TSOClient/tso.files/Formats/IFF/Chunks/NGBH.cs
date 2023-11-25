using FSO.Files.Utils;
using System.Collections.Generic;
using System.IO;

namespace FSO.Files.Formats.IFF.Chunks
{
    /// <summary>
    /// This chunk type contains general neighbourhood data within a neighbourhood.iff file.
    /// The only thing this was used for initially was tracking the tutorial.
    /// 
    /// As of hot date, it also includes inventory data, which was added as something of an afterthought.
    /// </summary>
    public class NGBH : IffChunk
    {
        public short[] NeighborhoodData = new short[16];
        public Dictionary<short, List<InventoryItem>> InventoryByID = new Dictionary<short, List<InventoryItem>>();

        public uint Version;

        /// <summary>
        /// Reads a NGBH chunk from a stream.
        /// </summary>
        /// <param name="iff">An Iff instance.</param>
        /// <param name="stream">A Stream object holding a OBJf chunk.</param>
        public override void Read(IffFile iff, Stream stream)
        {
            using (var io = IoBuffer.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.ReadUInt32(); //pad
                Version = io.ReadUInt32(); //0x49 for latest game
                string magic = io.ReadCString(4); //HBGN

                for (int i=0; i<16; i++)
                {
                    NeighborhoodData[i] = io.ReadInt16();
                }

                if (!io.HasMore) return; //no inventory present (yet)
                var count = io.ReadInt32();
                for (int i = 0; i < count; i++)
                {
                    if (io.ReadInt32() != 1) { }
                    var neighID = io.ReadInt16();
                    var inventoryCount = io.ReadInt32();
                    var inventory = new List<InventoryItem>();

                    for (int j=0; j<inventoryCount; j++)
                    {
                        inventory.Add(new InventoryItem(io));
                    }
                    InventoryByID[neighID] = inventory;
                }
            }
        }

        public override bool Write(IffFile iff, Stream stream)
        {
            using (var io = IoWriter.FromStream(stream, ByteOrder.LITTLE_ENDIAN))
            {
                io.WriteInt32(0);
                io.WriteInt32(0x49);
                io.WriteCString("HBGN", 4);
                
                for (int i=0; i<NeighborhoodData.Length; i++)
                {
                    io.WriteInt16(NeighborhoodData[i]);
                }

                io.WriteInt32(InventoryByID.Count);
                foreach (var item in InventoryByID)
                {
                    io.WriteInt32(1);
                    io.WriteInt16(item.Key);
                    io.WriteInt32(item.Value.Count);
                    foreach (var invent in item.Value)
                    {
                        invent.SerializeInto(io);
                    }
                }
            }
            return true;
        }
    }

    public class InventoryItem
    {
        public int Type;
        public uint GUID;
        public ushort Count;

        public InventoryItem() { }

        public InventoryItem(IoBuffer io)
        {
            Type = io.ReadInt32();
            GUID = io.ReadUInt32();
            Count = io.ReadUInt16();
        }

        public void SerializeInto(IoWriter io)
        {
            io.WriteInt32(Type);
            io.WriteUInt32(GUID);
            io.WriteUInt16(Count);
        }

        public InventoryItem Clone()
        {
            return new InventoryItem() { Type = Type, GUID = GUID, Count = Count };
        }

        public override string ToString()
        {
            return "Type: "+Type+", GUID: "+GUID+", Count: "+Count;
        }
    }
}
