using FSO.SimAntics.Marshals;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMStateSyncCmd : VMNetCommandBodyAbstract
    {
        public VMMarshal State;

        public override bool Execute(VM vm)
        {
            vm.Load(State);
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return !FromNet;
        }

        #region VMSerializable Members
        public override void Deserialize(BinaryReader reader)
        {
            State = new VMMarshal();
            State.Deserialize(reader);
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            State.SerializeInto(writer);
        }
        #endregion
    }
}
