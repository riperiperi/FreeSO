using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Model.TSOPlatform
{
    public class VMTSOEntityState : VMPlatformState
    {
        public VMBudget Budget = new VMBudget();
        public override void Deserialize(BinaryReader reader)
        {
            Budget.Deserialize(reader);
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            Budget.SerializeInto(writer);
        }

        public override void Tick(VM vm, object owner)
        {
        }
    }
}
