using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.LotView.Model;

namespace FSO.SimAntics.Marshals
{
    public class VMMultitileGroupMarshal : VMSerializable
    {
        public bool MultiTile;
        public short[] Objects;
        public LotTilePos[] Offsets;

        public void Deserialize(BinaryReader reader)
        {
            MultiTile = reader.ReadBoolean();

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
            writer.Write(Objects.Length);
            foreach (var item in Objects) writer.Write(item);
            foreach (var item in Offsets) item.SerializeInto(writer);
        }
    }
}
