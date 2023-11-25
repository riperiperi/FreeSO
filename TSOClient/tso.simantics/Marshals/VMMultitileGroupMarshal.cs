using FSO.SimAntics.NetPlay.Model;
using System.IO;
using FSO.LotView.Model;

namespace FSO.SimAntics.Marshals
{
    public class VMMultitileGroupMarshal : VMSerializable
    {
        public bool MultiTile;
        public string Name;
        public int Price;
        public int SalePrice = -1;
        public short[] Objects;
        public LotTilePos[] Offsets;
        public int Version;

        public VMMultitileGroupMarshal() { }
        public VMMultitileGroupMarshal(int version) { Version = version; }

        public void Deserialize(BinaryReader reader)
        {
            MultiTile = reader.ReadBoolean();
            Name = reader.ReadString();
            Price = reader.ReadInt32();
            if (Version > 12) SalePrice = reader.ReadInt32();

            var objs = reader.ReadInt32();
            Objects = new short[objs];
            for (int i=0; i<objs; i++) Objects[i] = reader.ReadInt16();

            Offsets = new LotTilePos[objs];
            for (int i = 0; i < objs; i++)
            {
                Offsets[i] = new LotTilePos();
                Offsets[i].Deserialize(reader);
            }
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(MultiTile);
            writer.Write(Name);
            writer.Write(Price);
            writer.Write(SalePrice);
            writer.Write(Objects.Length);
            writer.Write(VMSerializableUtils.ToByteArray(Objects));
            foreach (var item in Offsets) item.SerializeInto(writer);
        }
    }
}
