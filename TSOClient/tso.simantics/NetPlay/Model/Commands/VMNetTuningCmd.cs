using FSO.Common.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            Tuning = new DynamicTuning(Tuning);
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            Tuning.SerializeInto(writer);
        }
    }
}
