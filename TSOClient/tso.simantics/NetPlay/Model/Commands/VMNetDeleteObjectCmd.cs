/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using FSO.LotView.Model;
using FSO.SimAntics.Model;
using FSO.SimAntics.Primitives;
using FSO.SimAntics.Model.TSOPlatform;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetDeleteObjectCmd : VMNetCommandBodyAbstract
    {
        public short ObjectID;
        public uint ObjectPID;
        public bool CleanupAll;
        public bool Verified;
        public bool Success;
        public override bool Execute(VM vm, VMAvatar caller)
        {
            if (ObjectPID == 0) //only has value when this is an inventory move.
            {
                VMEntity obj = vm.GetObjectById(ObjectID);
                if (obj == null || (!vm.TS1 && caller == null)) return false;
                obj.Delete(CleanupAll, vm.Context);

                // If we're the server, tell the global link to give their money back.
                if (vm.GlobalLink != null)
                {
                    vm.GlobalLink.PerformTransaction(vm, false, uint.MaxValue, caller?.PersistID ?? uint.MaxValue, obj.MultitileGroup.Price,
                    (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                    {

                    });
                }
                vm.SignalChatEvent(new VMChatEvent(caller, VMChatEventType.Arch,
                    caller?.Name ?? "Unknown",
                    vm.GetUserIP(caller?.PersistID ?? 0),
                    "deleted " + obj.ToString()
                ));
            }
            else
            {
                //inventory move. Just delete the object.
                VMEntity obj = vm.GetObjectByPersist(ObjectPID);
                if (obj == null) return false;

                if (Success)
                {
                    if (((VMTSOObjectState)obj.TSOState).OwnerID == vm.MyUID)
                    {
                        //if the owner is here, tell them this object is now in their inventory.
                        //if they're elsewhere, they'll see it the next time their inventory updates. 
                        //Inventory accesses are not by index, but PID, and straight to DB... so this won't cause any race conditions.
                        vm.MyInventory.Add(new VMInventoryItem()
                        {
                            ObjectPID = ObjectPID,
                            GUID = (obj.MasterDefinition?.GUID) ?? obj.Object.OBJ.GUID,
                            Name = obj.MultitileGroup.Name,
                            Value = (uint)obj.MultitileGroup.Price,
                            Graphic = (ushort)obj.GetValue(VMStackObjectVariable.Graphic),
                            DynFlags1 = obj.DynamicSpriteFlags,
                            DynFlags2 = obj.DynamicSpriteFlags2,
                        });
                    }
                    vm.Context.ObjectQueries.RemoveMultitilePersist(vm, obj.PersistID);
                    foreach (var o in obj.MultitileGroup.Objects) o.PersistID = 0; //no longer representative of the object in db.
                    obj.Delete(CleanupAll, vm.Context);

                    vm.SignalChatEvent(new VMChatEvent(caller, VMChatEventType.Arch,
                        caller?.Name ?? "disconnected user",
                        vm.GetUserIP(caller.PersistID),
                        "sent " + obj.ToString() + " back to the inventory of its owner."
                    ));
                } else
                {
                    //something bad happened. just unlock the object.
                    foreach (var o in obj.MultitileGroup.Objects)
                        ((VMGameObject)o).Disabled &= VMGameObjectDisableFlags.TransactionIncomplete;
                }
            }

            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (Verified) return true;
            ObjectPID = 0;
            VMEntity obj = vm.GetObjectById(ObjectID);
            if (!vm.TS1)
            {
                if (caller == null) return false;
                var permissions = caller.AvatarState.Permissions;
                if (permissions < VMTSOAvatarPermissions.Roommate) return false;
                if (obj != null && permissions == VMTSOAvatarPermissions.Admin)
                {
                    VMNetLockCmd.LockObj(vm, obj);
                    return true; //admins can always deete
                }
            }
            if (obj == null || (obj is VMAvatar) || obj.IsUserMovable(vm.Context, true) != VMPlacementError.Success) return false;
            if ((((VMGameObject)obj).Disabled & VMGameObjectDisableFlags.TransactionIncomplete) > 0) return false; //can't delete objects mid trasaction...
            VMNetLockCmd.LockObj(vm, obj);

            var canDelete = vm.TS1 || obj.PersistID == 0 || ((VMTSOObjectState)obj.TSOState).OwnerID == caller.PersistID;
            if (canDelete)
            {
                //straight up delete this object. another check will be done at the execution stage.
                return true;
            } else
            {
                //send to the owner's inventory
                ObjectPID = obj.PersistID; //we don't want to accidentally delete the wrong object - so use its real persist id.
                vm.GlobalLink.MoveToInventory(vm, obj.MultitileGroup, (bool success, uint pid) =>
                {
                    Success = success;
                    Verified = true;
                    vm.ForwardCommand(this);
                });
                return false;
            }
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(ObjectID);
            writer.Write(ObjectPID);
            writer.Write(CleanupAll);
            writer.Write(Success);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ObjectID = reader.ReadInt16();
            ObjectPID = reader.ReadUInt32();
            CleanupAll = reader.ReadBoolean();
            Success = reader.ReadBoolean();
        }

        #endregion
    }
}
