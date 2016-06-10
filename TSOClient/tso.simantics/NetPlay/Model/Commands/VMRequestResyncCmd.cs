using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMRequestResyncCmd : VMNetCommandBodyAbstract
    {
        public uint Reason;
        public override bool Execute(VM vm)
        {
            //currently handled in the server driver. might want to make this custom packet when we move to electron.
            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Reason);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Reason = reader.ReadUInt32();
        }
        #endregion
    }
}
