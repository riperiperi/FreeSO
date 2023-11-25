using FSO.SimAntics.Model.Platform;
using System.IO;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.SimAntics.Model.TS1Platform
{
    public class VMTS1AvatarState : VMTS1EntityState, VMIAvatarState
    {
        public VMTS1AvatarState() { }
        public VMTS1AvatarState(int version) : base(version) { }

        public VMTSOAvatarPermissions Permissions
        {
            get; set;
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            Permissions = (VMTSOAvatarPermissions)reader.ReadByte();
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write((byte)Permissions);
        }

        public override void Tick(VM vm, object owner)
        {
            base.Tick(vm, owner);
        }
    }
}
