using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace FSO.SimAntics.Model
{
    public class VMBudget : VMSerializable
    {
        public uint Value = 0; 
        public bool CanTransact(int value)
        {
            return (Value + value >= 0);
        }

        public bool Transaction(int value)
        {
            if (!CanTransact(value)) return false;
            Value = (uint)(Value + value);
            return true;
        }

        public void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadUInt32();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Value);
        }
    }
}
