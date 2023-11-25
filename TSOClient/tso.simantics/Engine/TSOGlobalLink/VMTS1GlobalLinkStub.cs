using System;
using System.Collections.Generic;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Primitives;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Engine.TSOGlobalLink.Model;
using FSO.SimAntics.NetPlay.EODs.Handlers;

namespace FSO.SimAntics.Engine.TSOTransaction
{
    /// <summary>
    /// Does not access an external database. Does the best it can with the information it currently has.
    /// Changes made do not persist to database, and can be overwritten very easily.
    /// In the final setup, should only be used for client check trees.
    /// </summary>
    public class VMTS1GlobalLinkStub : IVMTSOGlobalLink
    {
        private Queue<VMNetArchitectureCmd> ArchBuffer = new Queue<VMNetArchitectureCmd>();
        private bool WaitingOnArch;

        public void PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount, short thread, short type, VMAsyncTransactionCallback callback)
        {
            var result = PerformTransaction(vm, testOnly, uid1, uid2, amount);
            if (callback != null)
            {
                var obj1 = vm.GetObjectByPersist(uid1);
                var obj2 = vm.GetObjectByPersist(uid2);
                var finalAmount = amount;

                new System.Threading.Thread(() =>
                {
                    //update client side budgets for avatars involved.
                    vm.SendCommand(new VMNetAsyncResponseCmd(thread, new VMTransferFundsState
                    {
                        Responded = true,
                        Success = result,
                        TransferAmount = finalAmount,
                        UID1 = uid1,
                        Budget1 = (obj1 == null) ? 0 : GetBudgetForFamily(vm, obj1),
                        UID2 = uid2,
                        Budget2 = (obj2 == null) ? 0 : GetBudgetForFamily(vm, obj2)
                    }));

                    callback(result, finalAmount,
                        uid1, (obj1 == null) ? 0 : GetBudgetForFamily(vm, obj1),
                        uid2, (obj2 == null) ? 0 : GetBudgetForFamily(vm, obj2));
                }).Start();
            }
        }

        public void PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount, short type, VMAsyncTransactionCallback callback)
        {
            PerformTransaction(vm, testOnly, uid1, uid2, amount, type, 0, callback);
        }

        public void PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount, VMAsyncTransactionCallback callback)
        {
            PerformTransaction(vm, testOnly, uid1, uid2, amount, 0, 0, callback);
        }

        public uint GetBudgetForFamily(VM vm, VMEntity ent)
        {
            if (ent != null && ent is VMAvatar)
            {
                //todo: make this get the appropriate family
                //if multiple families are playable in the same lot
                return (uint?)vm.TS1State.CurrentFamily?.Budget ?? uint.MaxValue;
            }
            return uint.MaxValue; //maxis has infinite money
        }

        public bool TransactBudgetForFamily(VM vm, VMEntity ent, int delta)
        {
            if (ent != null && ent is VMAvatar)
            {
                //todo: make this get the appropriate family
                //if multiple families are playable in the same lot
                if (vm.TS1State.CurrentFamily != null)
                {
                    if (vm.TS1State.CurrentFamily.Budget + delta < 0) return false;
                    vm.TS1State.CurrentFamily.Budget += delta;
                }
                return true;
            }
            return true; //maxis has infinite money
        }

        public bool CanTransactBudgetForFamily(VM vm, VMEntity ent, int delta)
        {
            if (ent != null && ent is VMAvatar)
            {
                //todo: make this get the appropriate family
                //if multiple families are playable in the same lot
                if (vm.TS1State.CurrentFamily != null)
                {
                    if (vm.TS1State.CurrentFamily.Budget + delta < 0) return false;
                }
                return true;
            }
            return true; //maxis has infinite money
        }

        public bool PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount)
        {
            var obj1 = vm.GetObjectByPersist(uid1);
            var obj2 = vm.GetObjectByPersist(uid2);

            // max value ID is "maxis"

            // fail if the target is an object and they're not on lot
            if (obj1 != null && !CanTransactBudgetForFamily(vm, obj1, -amount)) return false;
            if (obj2 != null && !CanTransactBudgetForFamily(vm, obj2, amount)) return false;

            if (!testOnly)
            {
                if (obj1 != null) TransactBudgetForFamily(vm, obj1, -amount);
                if (obj2 != null) TransactBudgetForFamily(vm, obj2, amount);
            }
            return true;
        }

        public void QueueArchitecture(VMNetArchitectureCmd cmd)
        {
            lock (ArchBuffer)
            {
                ArchBuffer.Enqueue(cmd);
            }
        }

        public void Tick(VM vm)
        {
            lock (ArchBuffer)
            {
                while (!WaitingOnArch && ArchBuffer.Count > 0)
                {
                    var cmd = ArchBuffer.Dequeue();
                    var cost = vm.Context.Architecture.SimulateCommands(cmd.Commands, false);
                    if (cost == 0)
                    {
                        //just send it
                        cmd.Verified = true;
                        vm.ForwardCommand(cmd);
                    }
                    else
                    {
                        uint source, target;
                        if (cost > 0) { source = cmd.ActorUID; target = uint.MaxValue; }
                        else { source = uint.MaxValue; target = cmd.ActorUID; }
                        WaitingOnArch = true;
                        PerformTransaction(vm, false, source, target, Math.Abs(cost),
                            (bool success, int transferAmount, uint uid1, uint budget1, uint uid2, uint budget2) =>
                            {
                                lock (ArchBuffer) WaitingOnArch = false;
                                if (success)
                                {
                                    cmd.Verified = true;
                                    vm.ForwardCommand(cmd);
                                }
                            });
                    }
                }
            }
        }

        public void LeaveLot(VM vm, VMAvatar avatar)
        {
            //TODO: in the global server, this will save the avatar (and possibly lot) states and send back to server.
            if (avatar.PersistID == vm.MyUID)
            {
                //stub has some functionality here. if we have left lot, disconnect.
                vm.CloseNet(VMCloseNetReason.LeaveLot);
            }
            avatar.Delete(true, vm.Context);
            vm.Context.VM.SignalChatEvent(new VMChatEvent(avatar, VMChatEventType.Leave, avatar.Name));
        }

        public void RequestRoommate(VM vm, uint pid, int mode, byte permissions)
        {
            //in final game: signal to city server persistant roommate request state.
            //right now: immedaiately add as roommate
            vm.ForwardCommand(new VMChangePermissionsCmd()
            {
                TargetUID = pid,
                Level = VMTSOAvatarPermissions.Roommate,
                Verified = true
            });
        }

        public void RemoveRoommate(VM vm, VMAvatar avatar)
        {
            //in final game: signal to city server persistant roommate request state.
            //right now: immedaiately remove
            vm.ForwardCommand(new VMChangePermissionsCmd()
            {
                TargetUID = avatar.PersistID,
                Level = VMTSOAvatarPermissions.Visitor,
                Verified = true
            });
        }

        public void ObtainAvatarFromTicket(VM vm, string ticket, VMAsyncAvatarCallback callback)
        {

        }

        public void LoadPluginPersist(VM vm, uint objectPID, uint pluginID, VMAsyncPluginLoadCallback callback)
        {
            new System.Threading.Thread(() =>
            {
                callback(null);
            }).Start();
        }

        public void SavePluginPersist(VM vm, uint objectPID, uint pluginID, byte[] data)
        {
            //Database.SavePluginPersist(objectPID, pluginID, data);
        }

        public uint NextPersist = 0x1000000;
        public void RegisterNewObject(VM vm, VMEntity obj, VMAsyncPersistIDCallback callback)
        {
            //todo: sandbox servers should give things an "id"
            vm.SendCommand(new VMNetUpdatePersistStateCmd()
            {
                ObjectID = obj.ObjectID,
                PersistID = NextPersist++
            });
        }

        public void MoveToInventory(VM vm, VMMultitileGroup obj, VMAsyncInventorySaveCallback callback)
        {
            callback(true, obj.BaseObject.PersistID);
            //todo: nice stub for this using database?
        }

        public void PurchaseFromOwner(VM vm, VMMultitileGroup obj, uint purchaserPID, VMAsyncInventorySaveCallback callback, VMAsyncTransactionCallback tcallback)
        {
            callback(true, obj.BaseObject.PersistID);
            //todo: nice stub for this using database?
        }

        public void RetrieveFromInventoryByType(VM vm, uint ownerPID, uint guid, int index, bool setOnLot, VMAsyncInventoryRetrieveCallback callback)
        {
            //todo: nice stub for this using database?
            callback(new VMInventoryRestoreObject(0, 0, 0));
        }

        public void RetrieveFromInventory(VM vm, uint objectPID, uint ownerPID, bool setOnLot, VMAsyncInventoryRetrieveCallback callback)
        {
            //todo: nice stub for this using database?
            callback(new VMInventoryRestoreObject(0, 0, 0));
        }

        public void ForceInInventory(VM vm, uint objectPID, VMAsyncInventorySaveCallback callback)
        {
            //todo: nice stub for this using database?
        }

        public void DeleteObject(VM vm, uint objectPID, VMAsyncDeleteObjectCallback callback)
        {
            //todo: delete local data
        }

        public void ConsumeInventory(VM vm, uint ownerPID, uint guid, int mode, short num, VMAsyncInventoryConsumeCallback callback)
        {
            //todo: nice stub for this using database?
        }

        public void SetSpotlightStatus(VM vm, bool on)
        {

        }

        public void StockOutfit(VM vm, VMGLOutfit outfit, VMAsyncStockOutfitCallback callback)
        {
            //todo: local stub?
        }

        public void GetOutfits(VM vm, VMGLOutfitOwner owner, uint ownerPID, VMAsyncGetOutfitsCallback callback)
        {
            callback(new VMGLOutfit[0]);
        }

        public void DeleteOutfit(VM vm, uint outfitPID, VMGLOutfitOwner owner, uint ownerPID, VMAsyncDeleteOutfitCallback callback)
        {
        }

        public void UpdateOutfitSalePrice(VM vm, uint outfitPID, uint objectPID, int newSalePrice, VMAsyncUpdateOutfitSalePriceCallback callback)
        {

        }

        public void PurchaseOutfit(VM vm, uint outfitPID, uint objectPID, uint avatarPID, VMAsyncPurchaseOutfitCallback callback)
        {
        }

        public void UpdateObjectPersist(VM vm, VMMultitileGroup obj, VMAsyncInventorySaveCallback callback)
        {

        }
        public void GetDynPayouts(VMAsyncNewspaperCallback callback)
        {
        }

        public void SecureTrade(VM vm, VMEODSecureTradePlayer p1, VMEODSecureTradePlayer p2, VMAsyncSecureTradeCallback callback)
        {
        }

        public void FindLotAndValue(VM vm, uint persistID, VMAsyncFindLotCallback p)
        {
        }

        public void GetBulletinState(VM vm, VMAsyncBulletinCallback callback)
        {

        }

        public void TokenRequest(VM vm, uint avatarID, uint guid, VMTokenRequestMode mode, List<int> attributeData, VMAsyncTokenCallback callback)
        {
            callback(true, attributeData);
        }

        public void GetObjectGlobalCooldown(VM vm, uint objectGUID, uint avatarID, uint userID, TimeSpan cooldownLength, bool considerAccount, bool considerCategory, VMAsyncGetObjectCooldownCallback callback)
        {

        }

        public void GetAccountIDFromAvatar(uint avatarID, VMAsyncAccountUserIDFromAvatarCallback callback)
        {

        }

        public void SecureTrade(VM vm, VMEODSecureTradePlayer p1, VMEODSecureTradePlayer p2, List<uint> untradableGUIDs, VMAsyncSecureTradeCallback callback)
        {

        }

        public void FindLotAndValue(VM vm, uint persistID, List<uint> untradableGUIDs, VMAsyncFindLotCallback p)
        {

        }
    }
}
