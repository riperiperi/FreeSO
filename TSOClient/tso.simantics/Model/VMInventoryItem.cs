using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace FSO.SimAntics.Model
{
    public class VMInventoryItem : VMSerializable
    {
        public uint ObjectPID;
        public string Name;
        public uint GUID;
        public ushort Graphic;
        public uint Value;
        public ulong DynFlags1;
        public ulong DynFlags2;

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(ObjectPID);
            writer.Write(Name);
            writer.Write(GUID);
            writer.Write(Graphic);
            writer.Write(Value);
            writer.Write(DynFlags1);
            writer.Write(DynFlags2);
        }

        public void Deserialize(BinaryReader reader)
        {
            ObjectPID = reader.ReadUInt32();
            Name = reader.ReadString();
            GUID = reader.ReadUInt32();
            Graphic = reader.ReadUInt16();
            Value = reader.ReadUInt32();
            DynFlags1 = reader.ReadUInt64();
            DynFlags2 = reader.ReadUInt64();
        }
    }
}
