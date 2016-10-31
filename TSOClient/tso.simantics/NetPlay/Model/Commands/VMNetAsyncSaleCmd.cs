using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.SimAntics.NetPlay.Model.Commands
{
    public class VMNetAsyncSaleCmd : VMNetCommandBodyAbstract
    {
        public uint ObjectPID;
        public bool Success;
        public bool Verified;

        public override bool Execute(VM vm, VMAvatar caller)
        {
            //sale and inventory move completed in verification, delete it now.

            var obj = vm.GetObjectByPersist(ObjectPID);
            if (obj != null && obj is VMGameObject)
            {
                if (Success)
                {
                    //is this my sim's object? try add it to our local inventory representaton
                    if (caller.PersistID == vm.MyUID)
                    {
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
                    obj.PersistID = 0; //no longer representative of the object in db.
                    obj.Delete(true, vm.Context);
                    if (VM.UseWorld) HIT.HITVM.Get().PlaySoundEvent("ui_letter_send");
                } else
                {
                    foreach (var o in obj.MultitileGroup.Objects)
                        ((VMGameObject)o).Disabled &= VMGameObjectDisableFlags.TransactionIncomplete;
                }
            }


            return true;
        }

        public override bool Verify(VM vm, VMAvatar caller)
        {
            if (Verified) return true;
            if (ObjectPID == 0) return false;
            var targObj = vm.GetObjectByPersist(ObjectPID);
            if (targObj == null) return false;
            if (caller == null //caller must be on lot
                || targObj.PersistID == 0 || targObj is VMAvatar || targObj.IsUserMovable(vm.Context, true) != VMPlacementError.Success)
                return false;

            if ((((VMGameObject)targObj).Disabled & VMGameObjectDisableFlags.ForSale) == 0) return false; //must be for sale to buy it
            if ((((VMGameObject)targObj).Disabled & VMGameObjectDisableFlags.TransactionIncomplete) > 0) return false; //someone else can't be buying it

            VMNetLockCmd.LockObj(vm, targObj);

            vm.GlobalLink.PurchaseFromOwner(vm, targObj.MultitileGroup, caller.PersistID, (bool success, uint pid) =>
            {
                Success = success;
                Verified = true;
                vm.ForwardCommand(this);
            },
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
            return false;
        }

        public override void Deserialize(BinaryReader reader)
        {
            base.Deserialize(reader);
            ObjectPID = reader.ReadUInt32();
            Success = reader.ReadBoolean();
        }

        public override void SerializeInto(BinaryWriter writer)
        {
            base.SerializeInto(writer);
            writer.Write(ObjectPID);
            writer.Write(Success);
        }
    }
}
