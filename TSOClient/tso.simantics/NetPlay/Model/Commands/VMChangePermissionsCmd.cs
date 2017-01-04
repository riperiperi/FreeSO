using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Model;
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
            var obj = vm.GetAvatarByPersist(TargetUID);
            var roomieChange = false;
            if (obj == null)
            {
                //todo: changing owner for off-lot users, though you really shouldn't be doing that.
                vm.TSOState.BuildRoommates.Remove(TargetUID);
                if (vm.TSOState.Roommates.Contains(TargetUID)) roomieChange = true;
                vm.TSOState.Roommates.Remove(TargetUID);
                if (Level >= VMTSOAvatarPermissions.Roommate && Level < VMTSOAvatarPermissions.Admin)
                {
                    roomieChange = !roomieChange;
                    vm.TSOState.Roommates.Add(TargetUID);
                    if (Level > VMTSOAvatarPermissions.Roommate) vm.TSOState.BuildRoommates.Add(TargetUID);
                }
            }
            else
            {

                var oldState = ((VMTSOAvatarState)obj.TSOState).Permissions;

                /*if (vm.GlobalLink != null && oldState >= VMTSOAvatarPermissions.Admin)
                    ((VMTSOGlobalLinkStub)vm.GlobalLink).Database.Administrators.Remove(obj.PersistID);*/

                if (oldState >= VMTSOAvatarPermissions.Roommate)
                {
                    vm.TSOState.Roommates.Remove(obj.PersistID);
                    roomieChange = !roomieChange;
                    ((VMTSOAvatarState)obj.TSOState).Flags |= VMTSOAvatarFlags.CanBeRoommate;
                }
                if (oldState >= VMTSOAvatarPermissions.BuildBuyRoommate) vm.TSOState.BuildRoommates.Remove(obj.PersistID);
                ((VMTSOAvatarState)obj.TSOState).Permissions = Level;
                if (Level >= VMTSOAvatarPermissions.Roommate)
                {
                    ((VMTSOAvatarState)obj.TSOState).Flags &= ~VMTSOAvatarFlags.CanBeRoommate;
                    roomieChange = !roomieChange; //flips roomie change back
                    vm.TSOState.Roommates.Add(obj.PersistID);
                }
                if (Level >= VMTSOAvatarPermissions.BuildBuyRoommate) vm.TSOState.BuildRoommates.Add(obj.PersistID);

                /*if (vm.GlobalLink != null && Level >= VMTSOAvatarPermissions.Admin)
                    ((VMTSOGlobalLinkStub)vm.GlobalLink).Database.Administrators.Add(obj.PersistID);*/
            }

            //mark objects not owned by roommates for inventory transfer
            if (roomieChange)
            {
                VMBuildableAreaInfo.UpdateOverbudgetObjects(vm);
                foreach (var ent in vm.Entities)
                {
                    if (ent is VMGameObject && ent.PersistID > 0 && ((VMTSOObjectState)ent.TSOState).OwnerID == TargetUID)
                    {
                        var old = ((VMGameObject)ent).Disabled;
                        if (Level < VMTSOAvatarPermissions.Roommate) ((VMGameObject)ent).Disabled |= VMGameObjectDisableFlags.PendingRoommateDeletion;
                        else ((VMGameObject)ent).Disabled &= ~VMGameObjectDisableFlags.PendingRoommateDeletion;
                        if (old != ((VMGameObject)ent).Disabled) vm.Scheduler.RescheduleInterrupt(ent);
                        ((VMGameObject)ent).RefreshLight();
                    }
                }
            }
            return base.Execute(vm);
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (Verified) return true;
            //can only change permissions to and from build roommate. caller must be owner, and target must be roomie/build (cannot change owner or admin)
            if (caller == null || //caller must be on lot, have owner permissions
                ((VMTSOAvatarState)caller.TSOState).Permissions < VMTSOAvatarPermissions.Owner)
                return false;

            if (Level > VMTSOAvatarPermissions.BuildBuyRoommate || Level < VMTSOAvatarPermissions.Roommate) return false; //can only switch to build roomie or back.

            if (!vm.TSOState.Roommates.Contains(TargetUID) || vm.TSOState.OwnerID == TargetUID) return false;

            /*
            var obj = vm.GetAvatarByPersist(TargetUID);
            if (obj == null
                || ((VMTSOAvatarState)caller.TSOState).Permissions > VMTSOAvatarPermissions.BuildBuyRoommate
                || ((VMTSOAvatarState)caller.TSOState).Permissions < VMTSOAvatarPermissions.Roommate
                || ((VMTSOAvatarState)caller.TSOState).Permissions == Level) return false;
                */

            vm.GlobalLink.RequestRoommate(vm, TargetUID, 3, (byte)((Level == VMTSOAvatarPermissions.Roommate) ? 0 : 1));

            return false;
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
