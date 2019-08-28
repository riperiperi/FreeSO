using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Primitives;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.Engine.TSOGlobalLink;
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
    public class VMTSOGlobalLinkStub : IVMTSOGlobalLink
    {
        private Queue<VMNetArchitectureCmd> ArchBuffer = new Queue<VMNetArchitectureCmd>();
        public VMTSOStandaloneDatabase Database; // = new VMTSOStandaloneDatabase();
        private bool WaitingOnArch;

        public void PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount, short type, short thread, VMAsyncTransactionCallback callback)
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
                        Budget1 = (obj1 == null) ? 0 : obj1.TSOState.Budget.Value,
                        UID2 = uid2,
                        Budget2 = (obj2 == null) ? 0 : obj2.TSOState.Budget.Value
                    }));
                    
                    callback(result, finalAmount,
                        uid1, (obj1 == null) ? 0 : obj1.TSOState.Budget.Value,
                        uid2, (obj2 == null) ? 0 : obj2.TSOState.Budget.Value);
                }).Start();
            }
        }

        public bool PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount)
        {
            var obj1 = vm.GetObjectByPersist(uid1);
            var obj2 = vm.GetObjectByPersist(uid2);

            // max value ID is "maxis"
            if ((uid1 != uint.MaxValue && obj1 == null) || (uid2 != uint.MaxValue && obj2 == null)) return false;

            // fail if the target is an object and they're not on lot
            if (obj1 != null && !obj1.TSOState.Budget.CanTransact(-amount)) return false;
            if (obj2 != null && !obj2.TSOState.Budget.CanTransact(amount)) return false;

            if (!testOnly)
            {
                if (obj1 != null) obj1.TSOState.Budget.Transaction(-amount);
                if (obj2 != null) obj2.TSOState.Budget.Transaction(amount);
            }
            return true;
        }

        public void PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount, short type, VMAsyncTransactionCallback callback)
        {
            PerformTransaction(vm, testOnly, uid1, uid2, amount, type, 0, callback);
        }

        public void PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount, VMAsyncTransactionCallback callback)
        {
            PerformTransaction(vm, testOnly, uid1, uid2, amount, 0, 0, callback);
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
            //gets an avatar from our stub database based on their ticket
            var uid = Database.FindOrAddAvatar(ticket);

            var permissions = VMTSOAvatarPermissions.Visitor;
            if (Database.Administrators.Contains(uid)) permissions = VMTSOAvatarPermissions.Admin;
            else if (vm.TSOState.BuildRoommates.Contains(uid)) permissions = VMTSOAvatarPermissions.BuildBuyRoommate;
            else if (vm.TSOState.Roommates.Contains(uid)) permissions = VMTSOAvatarPermissions.Roommate;

            //TODO!!!!!! This is a HACK to make sure SimJoin commands get sent AFTER the state sync.
            new System.Threading.Thread(() =>
            {
                System.Threading.Thread.Sleep(100);
                callback(uid, permissions);
            }).Start();
        }

        public void LoadPluginPersist(VM vm, uint objectPID, uint pluginID, VMAsyncPluginLoadCallback callback)
        {
            if (Database == null) callback(null);
            var dat = Database.LoadPluginPersist(objectPID, pluginID);

            new System.Threading.Thread(() =>
            {
                System.Threading.Thread.Sleep(100);
                callback(dat);
            }).Start();
        }

        public void SavePluginPersist(VM vm, uint objectPID, uint pluginID, byte[] data)
        {
            Database?.SavePluginPersist(objectPID, pluginID, data);
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
            callback(true, 0);
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
            callback(new VMEODFNewspaperData() {
                News = new List<VMEODFNewspaperNews>()
                {
                    new VMEODFNewspaperNews() {
                        ID = 0,
                        Name = "Test Event 1",
                        Description = "This event should show up as the latest event. It has a description " +
                        "which is too long to fit within the preview button, so the user has to click to " +
                        "expand it into the upper view."
                    },
                    new VMEODFNewspaperNews() {
                        ID = 1,
                        Name = "A Past Event",
                        Description = "This event should show up as a past event. It has a description " +
                        "which is too long to fit within the preview button, so the user has to click to " +
                        "expand it into the upper view."
                    },
                },
                Points = 
                new List<NetPlay.EODs.Handlers.VMEODFNewspaperPoint>()
            {
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 0, Multiplier = 1.0f, Skilltype = 0 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 0, Multiplier = 1.2f, Skilltype = 1 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 0, Multiplier = 1.3f, Skilltype = 2 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 0, Multiplier = 0.95f, Skilltype = 3 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 0, Multiplier = 0.66f, Skilltype = 4 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 0, Multiplier = 1.3f, Skilltype = 5 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 0, Multiplier = 0.8f, Skilltype = 6 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 0, Multiplier = 1.0f, Skilltype = 7 },

                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 1, Multiplier = 1.1f, Skilltype = 0 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 1, Multiplier = 1.3f, Skilltype = 1 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 1, Multiplier = 1.25f, Skilltype = 2 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 1, Multiplier = 1.05f, Skilltype = 3 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 1, Multiplier = 0.5f, Skilltype = 4 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 1, Multiplier = 1.5f, Skilltype = 5 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 1, Multiplier = 0.65f, Skilltype = 6 },
                new NetPlay.EODs.Handlers.VMEODFNewspaperPoint() { Day = 1, Multiplier = 0.9f, Skilltype = 7 },
            }
            });
        }

        public void SecureTrade(VM vm, VMEODSecureTradePlayer p1, VMEODSecureTradePlayer p2, VMAsyncSecureTradeCallback callback)
        {
        }

        public void FindLotAndValue(VM vm, uint persistID, VMAsyncFindLotCallback p)
        {
        }
        
        public void GetBulletinState(VM vm, VMAsyncBulletinCallback callback)
        {
            callback(0, 7);
        }
    }
}
