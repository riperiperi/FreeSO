using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TSO.Simantics.net.model.commands
{
    public class VMNetInteractionCancelCmd : VMNetCommandBodyAbstract
    {
        public ushort ActionUID;
        public short CallerID;

        public override bool Execute(VM vm)
        {
            VMEntity caller = vm.GetObjectById(CallerID);
            //TODO: check if net user owns caller!
            if (caller == null) return false;

            var interaction = caller.Thread.Queue.FirstOrDefault(x => x.UID == ActionUID);
            if (interaction != null) interaction.Cancelled = true;

            return true;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(ActionUID);
            writer.Write(CallerID);
        }

        public override void Deserialize(BinaryReader reader)
        {
            ActionUID = reader.ReadUInt16();
            CallerID = reader.ReadInt16();
        }

        #endregion
    }
}
