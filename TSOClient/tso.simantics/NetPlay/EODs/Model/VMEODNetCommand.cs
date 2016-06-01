using FSO.SimAntics.NetPlay.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.EODs.Model
{
    public class VMEODNetCommand : VMNetCommandBodyAbstract
    {
        public string EventName;
        public bool Binary;
        public string TextData;
        public byte[] BinData;

        public override bool Execute(VM vm)
        {
            //forward command to the sender avatar's plugin. (UI or Server Plugin)
            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(EventName);
            writer.Write(Binary);
            if (Binary)
            {
                writer.Write((ushort)BinData.Length);
                writer.Write(BinData);
            } else
            {
                writer.Write(TextData);
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            EventName = reader.ReadString();
            Binary = reader.ReadBoolean();
            if (Binary)
            {
                var length = reader.ReadUInt16();
                BinData = reader.ReadBytes(length);
            } else
            {
                TextData = reader.ReadString();
            }
        }
        #endregion
    }
}
