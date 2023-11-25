using FSO.SimAntics.NetPlay.Model;
using System;
using System.IO;

namespace FSO.SimAntics.NetPlay.EODs.Model
{
    public class VMEODEvent : VMSerializable
    {
        public short Code;
        public short[] Data;

        public VMEODEvent() { }
        public VMEODEvent(short code)
        {
            Code = code;
            Data = new short[0];
        }
        public VMEODEvent(short code, params short[] data) {
            Code = code;
            Data = data;
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Code);
            writer.Write((byte)Data.Length);
            foreach (var dat in Data) writer.Write(dat);
        }

        public void Deserialize(BinaryReader reader)
        {
            Code = reader.ReadInt16();
            var length = Math.Min((byte)4, reader.ReadByte());
            Data = new short[length];
            for (int i = 0; i < length; i++)
                Data[i] = reader.ReadInt16();
        }
    }
}
