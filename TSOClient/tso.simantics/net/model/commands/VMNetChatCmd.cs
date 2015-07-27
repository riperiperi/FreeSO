using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TSO.Simantics.net.model.commands
{
    public class VMNetChatCmd : VMNetCommandBodyAbstract
    {
        public short CallerID;
        public string Message;

        public override bool Execute(VM vm)
        {
            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(CallerID);
            writer.Write(Message);
        }

        public override void Deserialize(BinaryReader reader)
        {
            CallerID = reader.ReadInt16();
            Message = reader.ReadString();
        }
        #endregion
    }
}
