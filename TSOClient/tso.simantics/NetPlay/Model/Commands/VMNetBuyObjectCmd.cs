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

        private List<uint> Blacklist = new List<uint>
        {
            0x24C95F99
        };

        public override bool Execute(VM vm, VMAvatar caller)
        {
            var catalog = Content.Content.Get().WorldCatalog;
            var item = catalog.GetItemByGUID(GUID);
            if (Blacklist.Contains(GUID) || caller == null) return false;

            //careful here! if the object can't be placed, we have to give the user their money back.
            if (TryPlace(vm, caller)) return true;
            else if (vm.GlobalLink != null && item != null)
            { 
                vm.GlobalLink.PerformTransaction(vm, false, uint.MaxValue, caller.PersistID, (int)item.Price,
                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                    vm.SendCommand(new VMNetAsyncResponseCmd(0, new VMTransferFundsState
                    { //update budgets on clients. id of 0 means there is no target thread.
                        Responded = true,
                        Success = success,
                        TransferAmount = transferAmount,
                        UID1 = uid1,
                        Budget1 = budget1,
                        UID2 = uid2,
                        Budget2 = budget2
                    }));
                });
            }
            return false;
        }

        private bool TryPlace(VM vm, VMAvatar caller)
        {
            var catalog = Content.Content.Get().WorldCatalog;
            var item = catalog.GetItemByGUID(GUID);

            var group = vm.Context.CreateObjectInstance(GUID, new LotTilePos(x, y, level), dir);
            if (group == null) return false;
            group.ExecuteEntryPoint(11, vm.Context); //User Placement
            if (group.Objects.Count == 0) return false;
            if (group.BaseObject.Position == LotTilePos.OUT_OF_WORLD)
            {
                group.Delete(vm.Context);
                return false;
            }

            int salePrice = 0;
            if (item != null) salePrice = (int)item.Price;
            var def = group.BaseObject.MasterDefinition;
            if (def == null) def = group.BaseObject.Object.OBJ;
            var limit = def.DepreciationLimit;
            if (salePrice > limit) //only try to deprecate if we're above the limit. Prevents objects with a limit above their price being money fountains.
            {
                salePrice -= def.InitialDepreciation;
                if (salePrice < limit) salePrice = limit;
            }

            group.Price = (int)salePrice;
            foreach (var obj in group.Objects) {
                if (obj is VMGameObject) ((VMTSOObjectState)obj.TSOState).OwnerID = caller.PersistID;
            }

            vm.SignalChatEvent(new VMChatEvent(caller.PersistID, VMChatEventType.Arch,
                caller.Name,
                vm.GetUserIP(caller.PersistID),
                "placed " + group.BaseObject.ToString() + " at (" + x / 16f + ", " + y / 16f + ", " + level + ")"
            ));
            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (Verified) return true; //set internally when transaction succeeds. trust that the verification happened.
            if (caller == null || //caller must be on lot, have build permissions
                ((VMTSOAvatarState)caller.TSOState).Permissions < VMTSOAvatarPermissions.Roommate)
                return false;

            //get entry in catalog. first verify if it can be bought at all. (if not, error out)
            //TODO: error feedback for client
            var catalog = Content.Content.Get().WorldCatalog;
            var item = catalog.GetItemByGUID(GUID);

            if (item == null || item.Category == -1)
            {
                if (((VMTSOAvatarState)caller.TSOState).Permissions == VMTSOAvatarPermissions.Admin) return true;
                return false; //not purchasable
            }

            //TODO: fine grained purchase control based on user status

            //perform the transaction. If it succeeds, requeue the command
            vm.GlobalLink.PerformTransaction(vm, false, caller.PersistID, uint.MaxValue, (int)item.Price,
                (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                {
                    if (success)
                    {
                        Verified = true;
                        vm.ForwardCommand(this);
                    }
                    vm.SendCommand(new VMNetAsyncResponseCmd(0, new VMTransferFundsState
                    { //update budgets on clients. id of 0 means there is no target thread.
                        Responded = true,
                        Success = success,
                        TransferAmount = transferAmount,
                        UID1 = uid1,
                        Budget1 = budget1,
                        UID2 = uid2,
                        Budget2 = budget2
                    }));
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
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            GUID = reader.ReadUInt32();
            x = reader.ReadInt16();
            y = reader.ReadInt16();
            level = reader.ReadSByte();
            dir = (Direction)reader.ReadByte();
        }

        #endregion
    }
}
