using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Model.TSOPlatform;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMChangePermissionsCmd : VMNetCommandBodyAbstract
    {
        public uint TargetUID;
        public VMTSOAvatarPermissions Level;
        public bool Verified;
        public override bool Execute(VM vm)
        {
            var obj = vm.GetObjectByPersist(TargetUID);
            if (obj == null || obj is VMGameObject) return false;

            var oldState = ((VMTSOAvatarState)obj.TSOState).Permissions;

            if (vm.GlobalLink != null && oldState >= VMTSOAvatarPermissions.Admin)
                ((VMTSOGlobalLinkStub)vm.GlobalLink).Database.Administrators.Remove(obj.PersistID);
            if (oldState >= VMTSOAvatarPermissions.Roommate) vm.TSOState.Roommates.Remove(obj.PersistID);
            if (oldState >= VMTSOAvatarPermissions.BuildBuyRoommate) vm.TSOState.BuildRoommates.Remove(obj.PersistID);
            ((VMTSOAvatarState)obj.TSOState).Permissions = Level;
            if (Level >= VMTSOAvatarPermissions.Roommate) vm.TSOState.Roommates.Add(obj.PersistID);
            if (Level >= VMTSOAvatarPermissions.BuildBuyRoommate) vm.TSOState.BuildRoommates.Add(obj.PersistID);
            if (vm.GlobalLink != null && Level >= VMTSOAvatarPermissions.Admin)
                ((VMTSOGlobalLinkStub)vm.GlobalLink).Database.Administrators.Add(obj.PersistID);

            return base.Execute(vm);
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            return Verified;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(TargetUID);
            writer.Write((byte)Level);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            TargetUID = reader.ReadUInt32();
            Level = (VMTSOAvatarPermissions)reader.ReadByte();
        }

        #endregion
    }
}
