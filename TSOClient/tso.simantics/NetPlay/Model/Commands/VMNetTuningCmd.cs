using FSO.Common.Model;
using System.Collections.Generic;
using System.IO;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetTuningCmd : VMNetCommandBodyAbstract
    {
        public DynamicTuning Tuning;

        public override bool Execute(VM vm)
        {
            vm.Tuning = Tuning;
            vm.UpdateTuning();
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return !FromNet;
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Tuning = new DynamicTuning(reader);
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            if (Tuning == null) Tuning = new DynamicTuning(new List<DynTuningEntry>());
            Tuning.SerializeInto(writer);
        }
    }
}
