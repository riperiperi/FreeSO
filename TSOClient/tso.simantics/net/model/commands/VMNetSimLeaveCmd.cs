using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace TSO.Simantics.net.model.commands
{
    class VMNetSimLeaveCmd : VMNetCommandBodyAbstract
    {
        public uint SimID;

        public override bool Execute(VM vm)
        {
            var sim = vm.Entities.First(x => x is VMAvatar && x.PersistID == SimID);

            if (sim != null) sim.Delete(true, vm.Context);
            return true;
        }

        #region VMSerializable Members
        public override void SerializeInto(BinaryWriter writer)
        {
            writer.Write(SimID);
        }

        public override void Deserialize(BinaryReader reader)
        {
            SimID = reader.ReadUInt32();
        }
        #endregion
    }
}
