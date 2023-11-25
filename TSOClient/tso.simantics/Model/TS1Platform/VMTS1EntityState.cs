using FSO.SimAntics.Model.Platform;
using System.IO;

namespace FSO.SimAntics.Model.TS1Platform
{
    public class VMTS1EntityState : VMAbstractEntityState
    {
        public VMTS1EntityState() { }
        public VMTS1EntityState(int version) : base(version) { }

        public override void Deserialize(BinaryReader reader)
        {
        }

        public override void SerializeInto(BinaryWriter writer)
        {
        }

        public override void Tick(VM vm, object owner)
        {
        }
    }
}
