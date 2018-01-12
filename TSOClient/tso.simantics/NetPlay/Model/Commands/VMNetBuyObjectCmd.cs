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
using FSO.SimAntics.Primitives;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetBuyObjectCmd : VMNetCommandBodyAbstract
    {
        public uint GUID;
        public short x;
        public short y;
        public sbyte level;
        public Direction dir;
        public bool Verified;

        private int value = -1;

        private static HashSet<int> RoomieWhiteList = new HashSet<int>()
        {
            12, 13, 14, 15, 16, 17, 18, 19, 20
        };
        private static HashSet<int> BuilderWhiteList = new HashSet<int>()
        {
            12, 13, 14, 15, 16, 17, 18, 19, 20,
            0, 1, 2, 3, 4, 5, 7, 8, 9 //29 is terrain tool
        };

        private VMMultitileGroup CreatedGroup;

        private List<uint> Blacklist = new List<uint>
        {
            0x24C95F99
        };

        public override bool Execute(VM vm, VMAvatar caller)
        {
            var catalog = Content.Content.Get().WorldCatalog;
            var item = catalog.GetItemByGUID(GUID);
            if (!vm.TS1 && (Blacklist.Contains(GUID) || caller == null)) return false;

            //careful here! if the object can't be placed, we have to give the user their money back.
            if (TryPlace(vm, caller))
            {
                if (vm.GlobalLink != null)
                {
                    vm.GlobalLink.RegisterNewObject(vm, CreatedGroup.BaseObject, (short objID, uint pid) =>
                    {
                        vm.SendCommand(new VMNetUpdatePersistStateCmd()
                        {
                            ObjectID = objID,
                            PersistID = pid
                        });
                    });
                }

                //overwrite value

                var objDefinition = CreatedGroup.BaseObject.MasterDefinition ?? CreatedGroup.BaseObject.Object.OBJ;

                CreatedGroup.InitialPrice = (int)value;

                return true;
            }
            else if (vm.GlobalLink != null && item != null)
            {
                vm.GlobalLink.PerformTransaction(vm, false, uint.MaxValue, caller.PersistID, (int)value,
                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                    //check if we got the money back? there's really no reason for that to fail
                    //...and we can't exactly do much about it if it were to!
                });
            }
            return false;
        }

        private bool TryPlace(VM vm, VMAvatar caller)
        {
            if (!vm.TS1 && !vm.TSOState.CanPlaceNewUserObject(vm)) return false;
            var catalog = Content.Content.Get().WorldCatalog;
            var item = catalog.GetItemByGUID(GUID);

            var group = vm.Context.CreateObjectInstance(GUID, LotTilePos.OUT_OF_WORLD, dir);
            if (group == null) return false;
            group.ChangePosition(new LotTilePos(x, y, level), dir, vm.Context, VMPlaceRequestFlags.UserPlacement);
            group.ExecuteEntryPoint(11, vm.Context); //User Placement
            if (group.Objects.Count == 0) return false;
            if (group.BaseObject.Position == LotTilePos.OUT_OF_WORLD)
            {
                group.Delete(vm.Context);
                return false;
            }

            if (!vm.TS1)
            {
                foreach (var obj in group.Objects)
                {
                    if (obj is VMGameObject) ((VMTSOObjectState)obj.TSOState).OwnerID = caller.PersistID;
                }
            }
            CreatedGroup = group;

            vm.SignalChatEvent(new VMChatEvent(caller?.PersistID ?? 0, VMChatEventType.Arch,
                caller?.Name ?? "Unknown",
                vm.GetUserIP(caller?.PersistID ?? 0),
                "placed " + group.BaseObject.ToString() + " at (" + x / 16f + ", " + y / 16f + ", " + level + ")"
            ));
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (Verified) return true; //set internally when transaction succeeds. trust that the verification happened.
            value = 0; //do not trust value from net
            if (!vm.TS1)
            {
                if (caller == null || //caller must be on lot, have build permissions
                    ((VMTSOAvatarState)caller.TSOState).Permissions < VMTSOAvatarPermissions.Roommate ||
                    !vm.TSOState.CanPlaceNewUserObject(vm))
                    return false;
            }
            //get entry in catalog. first verify if it can be bought at all. (if not, error out)
            //TODO: error feedback for client
            var catalog = Content.Content.Get().WorldCatalog;
            var item = catalog.GetItemByGUID(GUID);

            if (!vm.TS1)
            {
                var whitelist = (((VMTSOAvatarState)caller.TSOState).Permissions == VMTSOAvatarPermissions.Roommate) ? RoomieWhiteList : BuilderWhiteList;
                if (item == null || !whitelist.Contains(item.Value.Category))
                {
                    if (((VMTSOAvatarState)caller.TSOState).Permissions == VMTSOAvatarPermissions.Admin) return true;
                    return false; //not purchasable
                }
            }
            
            if (item != null)
            {
                var price = (int)item.Value.Price;
                var dcPercent = VMBuildableAreaInfo.GetDiscountFor(item.Value, vm);
                value = (price * (100 - dcPercent)) / 100;
            }

            //TODO: fine grained purchase control based on user status

            //perform the transaction. If it succeeds, requeue the command
            vm.GlobalLink.PerformTransaction(vm, false, caller?.PersistID ?? uint.MaxValue, uint.MaxValue, value,
                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                    if (success)
                    {
                        Verified = true;
                        vm.ForwardCommand(this);
                    }
                });

            return false;
        }

        #region VMSerializable Members

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(GUID);
            writer.Write(x);
            writer.Write(y);
            writer.Write(level);
            writer.Write((byte)dir);
            writer.Write(value);
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            GUID = reader.ReadUInt32();
            x = reader.ReadInt16();
            y = reader.ReadInt16();
            level = reader.ReadSByte();
            dir = (Direction)reader.ReadByte();
            value = reader.ReadInt32();
        }

        #endregion
    }
}
