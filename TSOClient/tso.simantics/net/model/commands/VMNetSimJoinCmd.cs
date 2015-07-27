using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using tso.world.model;
using TSO.Simantics.primitives;

namespace TSO.Simantics.net.model.commands
{
    public class VMNetSimJoinCmd : VMNetCommandBodyAbstract
    {
        public uint SimID;

        public override bool Execute(VM vm)
        {
            var sim = vm.Context.CreateObjectInstance(VMAvatar.TEMPLATE_PERSON, LotTilePos.OUT_OF_WORLD, Direction.NORTH).Objects[0];
            var mailbox = vm.Entities.First(x => (x.Object.OBJ.GUID == 0xEF121974 || x.Object.OBJ.GUID == 0x1D95C9B0));

            VMFindLocationFor.FindLocationFor(sim, mailbox, vm.Context);
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
