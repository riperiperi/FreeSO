using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FSO.SimAntics.NetPlay.EODs.Model
{
    public class VMEODEvent : VMSerializable
    {
        public short Code;
        public byte[] Data;

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Code);
            writer.Write((byte)Data.Length);
            writer.Write(Data);
        }

        public void Deserialize(BinaryReader reader)
        {
            Code = reader.ReadInt16();
            var length = Math.Min((byte)4, reader.ReadByte());
            Data = reader.ReadBytes(length);
        }
    }
}
