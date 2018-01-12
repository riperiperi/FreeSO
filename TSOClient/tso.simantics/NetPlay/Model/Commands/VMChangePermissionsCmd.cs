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
        public VMChangePermissionsMode Mode;
        public uint ReplaceUID; //for object inherit modes. Set implicitly for owner replacement.
        public bool Verified;
        public override bool Execute(VM vm)
        {
            var obj = vm.GetAvatarByPersist(TargetUID);
            var roomieChange = false;
            if (Mode == VMChangePermissionsMode.OBJECTS_ONLY)
            {
                roomieChange = true;
            }
            else
            {
                var ownerSwitch = Mode == VMChangePermissionsMode.OWNER_SWITCH || Mode == VMChangePermissionsMode.OWNER_SWITCH_WITH_OBJECTS;
                if (ownerSwitch)
                {
                    ChangeUserLevel(vm, ReplaceUID, VMTSOAvatarPermissions.Visitor);
                }
                roomieChange = ChangeUserLevel(vm, TargetUID, Level);
                if (ownerSwitch) roomieChange = true;
            }

            //mark objects not owned by roommates for inventory transfer
            if (roomieChange)
            {
                VMBuildableAreaInfo.UpdateOverbudgetObjects(vm);
                var roomies = vm.TSOState.Roommates;
                foreach (var ent in vm.Entities)
                {
                    if (ent is VMGameObject && ent.PersistID > 0)
                    {
                        var owner = ((VMTSOObjectState)ent.TSOState).OwnerID;
                        if (owner == ReplaceUID && ReplaceUID != 0)
                        {
                            ((VMTSOObjectState)ent.TSOState).OwnerID = TargetUID;
                            owner = TargetUID;
                        }
                        var wasDisabled = (((VMGameObject)ent).Disabled & VMGameObjectDisableFlags.PendingRoommateDeletion) > 0;

                        var toBeDisabled = !roomies.Contains(owner);

                        if (wasDisabled != toBeDisabled)
                        {
                            if (toBeDisabled) ((VMGameObject)ent).Disabled |= VMGameObjectDisableFlags.PendingRoommateDeletion;
                            else ((VMGameObject)ent).Disabled &= ~VMGameObjectDisableFlags.PendingRoommateDeletion;
                            vm.Scheduler.RescheduleInterrupt(ent);
                            ((VMGameObject)ent).RefreshLight();
                        }
                    }
                }
            }
            return base.Execute(vm);
        }

        private bool ChangeUserLevel(VM vm, uint pid, VMTSOAvatarPermissions level)
        {
            var obj = vm.GetAvatarByPersist(pid);
            var roomieChange = false;
            if (obj == null)
            {
                vm.TSOState.BuildRoommates.Remove(pid);
                if (vm.TSOState.Roommates.Contains(pid)) roomieChange = true;
                vm.TSOState.Roommates.Remove(pid);
                if (level >= VMTSOAvatarPermissions.Roommate && level < VMTSOAvatarPermissions.Admin)
                {
                    roomieChange = !roomieChange;
                    vm.TSOState.Roommates.Add(pid);
                    if (level > VMTSOAvatarPermissions.Roommate) vm.TSOState.BuildRoommates.Add(pid);
                    if (level == VMTSOAvatarPermissions.Owner) vm.TSOState.OwnerID = pid;
                }
            }
            else
            {

                var oldState = ((VMTSOAvatarState)obj.TSOState).Permissions;

                if (oldState >= VMTSOAvatarPermissions.Roommate)
                {
                    vm.TSOState.Roommates.Remove(obj.PersistID);
                    roomieChange = !roomieChange;
                    ((VMTSOAvatarState)obj.TSOState).Flags |= VMTSOAvatarFlags.CanBeRoommate;
                }
                if (oldState >= VMTSOAvatarPermissions.BuildBuyRoommate) vm.TSOState.BuildRoommates.Remove(obj.PersistID);
                ((VMTSOAvatarState)obj.TSOState).Permissions = level;
                if (level >= VMTSOAvatarPermissions.Roommate)
                {
                    ((VMTSOAvatarState)obj.TSOState).Flags &= ~VMTSOAvatarFlags.CanBeRoommate;
                    roomieChange = !roomieChange; //flips roomie change back
                    vm.TSOState.Roommates.Add(obj.PersistID);
                }
                if (level >= VMTSOAvatarPermissions.BuildBuyRoommate) vm.TSOState.BuildRoommates.Add(obj.PersistID);
                if (level == VMTSOAvatarPermissions.Owner) vm.TSOState.OwnerID = pid;
                else if (vm.TSOState.OwnerID == pid) vm.TSOState.OwnerID = 0;
            }
            return roomieChange;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (Verified) return true;
            //can only change permissions to and from build roommate. caller must be owner, and target must be roomie/build (cannot change owner or admin)
            if (caller == null || //caller must be on lot, have owner permissions
                ((VMTSOAvatarState)caller.TSOState).Permissions < VMTSOAvatarPermissions.Owner)
                return false;

            if (Level > VMTSOAvatarPermissions.BuildBuyRoommate || Level < VMTSOAvatarPermissions.Roommate) return false; //users can only switch to build roomie or back.

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
            writer.Write(ReplaceUID);
            writer.Write((byte)Level);
            writer.Write((byte)Mode);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            TargetUID = reader.ReadUInt32();
            ReplaceUID = reader.ReadUInt32();
            Level = (VMTSOAvatarPermissions)reader.ReadByte();
            Mode = (VMChangePermissionsMode)reader.ReadByte();
        }

        #endregion
    }

    public enum VMChangePermissionsMode : byte
    {
        NORMAL = 0,
        OWNER_SWITCH,
        OWNER_SWITCH_WITH_OBJECTS,
        OBJECTS_ONLY
    }
}
