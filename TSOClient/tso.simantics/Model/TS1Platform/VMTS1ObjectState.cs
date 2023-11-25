using FSO.SimAntics.Model.Platform;
using System.IO;

namespace FSO.SimAntics.Model.TS1Platform
{
    public class VMTS1ObjectState : VMTS1EntityState, VMIObjectState
    {
        public ushort Wear
        {
            get; set;
        }

        public VMTS1ObjectState() { }
        public VMTS1ObjectState(int version) : base(version) { }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Wear = reader.ReadUInt16();
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(Wear);
        }

        public override void Tick(VM vm, object owner)
        {
            base.Tick(vm, owner);
        }

        public void ProcessQTRDay(VM vm, VMEntity owner)
        {
            
        }
    }
}
