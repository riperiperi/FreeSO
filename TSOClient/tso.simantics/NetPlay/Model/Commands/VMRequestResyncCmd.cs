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
        public uint TickID;

        public override bool Execute(VM vm)
        {
#if VM_DESYNC_DEBUG

#endif
            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(Reason);
            writer.Write(TickID);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Reason = reader.ReadUInt32();
            TickID = reader.ReadUInt32();
        }
        #endregion
    }
}
