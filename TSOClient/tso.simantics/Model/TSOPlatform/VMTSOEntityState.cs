using FSO.SimAntics.Model.Platform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Model.TSOPlatform
{
    public class VMTSOEntityState : VMAbstractEntityState
    {
        public VMBudget Budget = new VMBudget();

        public VMTSOEntityState() { }
        public VMTSOEntityState(int version) : base(version) { }

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
