using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FSO.LotView.Model;

namespace FSO.SimAntics.Marshals
{
    public class VMGameObjectMarshal : VMEntityMarshal
    {
        public Direction Direction;

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Direction = (Direction)reader.ReadByte();

            //var slots = reader.ReadInt32();
            //SlotContainees = new short[slots];
            //for (int i = 0; i < slots; i++) SlotContainees[i] = reader.ReadInt16();
        }
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write((byte)Direction);

            //writer.Write(SlotContainees.Length);
            //foreach (var item in SlotContainees) { writer.Write(item); }
        }
    }
}
