using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FSO.SimAntics.Marshals
{
    public class VMMultitileGroupMarshal : VMSerializable
    {
        public bool MultiTile;
        public short[] Objects;

        public void Deserialize(BinaryReader reader)
        {
            MultiTile = reader.ReadBoolean();
            var objs = reader.ReadInt32();
            Objects = new short[objs];
            for (int i=0; i<objs; i++) Objects[i] = reader.ReadInt16();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(MultiTile);
            writer.Write(Objects.Length);
            foreach (var item in Objects) writer.Write(item);
        }
    }
}
