﻿using FSO.Server.Database.DA;
using FSO.SimAntics.Engine.TSOTransaction;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.SimAntics;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.Primitives;
using System.IO;
using NLog;
using FSO.Server.Database.DA.Objects;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.Model;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Marshals;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.Roommates;
using FSO.SimAntics.Engine.TSOGlobalLink.Model;
using FSO.SimAntics.Engine.Scopes;
using FSO.Server.Database.DA.Avatars;
using FSO.SimAntics.NetPlay.EODs.Handlers;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Protocol.Gluon.Model;
using FSO.Server.Servers.Lot.Lifecycle;

namespace FSO.Server.Servers.Lot.Domain
{
    public class LotServerGlobalLink : IVMTSOGlobalLink
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private IDAFactory DAFactory;
        private ILotHost Host;
        private LotContext Context;
        private LotServerConfiguration Config;
        private Queue<VMNetArchitectureCmd> ArchBuffer = new Queue<VMNetArchitectureCmd>();
        private CityConnections City;
        private bool WaitingOnArch;

        public LotServerGlobalLink(LotServerConfiguration config, IDAFactory da, LotContext context, ILotHost host, CityConnections city)
        {
            DAFactory = da;
            Host = host;
            Context = context;
            Config = config;
            City = city;
        }

        public void LeaveLot(VM vm, VMAvatar avatar)
        {
            avatar.Delete(true, vm.Context);
            vm.Context.VM.SignalChatEvent(new VMChatEvent(avatar, VMChatEventType.Leave, avatar.Name));
        }

        public void PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount, VMAsyncTransactionCallback callback)
        {
            PerformTransaction(vm, testOnly, uid1, uid2, amount, 0, 0, callback);
        }

        public void PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount, short type, VMAsyncTransactionCallback callback)
        {
            PerformTransaction(vm, testOnly, uid1, uid2, amount, type, 0, callback);
        }

        public void PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount, short type, short thread, VMAsyncTransactionCallback callback)
        {
            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    var result = (testOnly)?db.Avatars.TestTransaction(uid1, uid2, amount, 0):db.Avatars.Transaction(uid1, uid2, amount, type);
                    if (result == null) result = new Database.DA.Avatars.DbTransactionResult() { success = false };

                    var finalAmount = amount;

                    //update client side budgets for avatars involved.
                    vm.SendCommand(new VMNetAsyncResponseCmd(thread, new VMTransferFundsState
                    {
                        Responded = true,
                        Success = result.success,
                        TransferAmount = result.amount,
                        UID1 = uid1,
                        Budget1 = (uint)result.source_budget,
                        UID2 = uid2,
                        Budget2 = (uint)result.dest_budget
                    }));

                    callback(result.success, result.amount,
                    uid1, (uint)result.source_budget,
                    uid2, (uint)result.dest_budget);
                }
            });
        }

        public void RequestRoommate(VM vm, uint avatarID, int mode, byte permissions)
        {
            //0 = initiate. 1 = accept. 2 = reject.
            //we have the "initiate" step so that users are reminded if they somehow dc before the transaction completes.
            //users will be reminded of their in-progress roommate request every time they log in or leave a lot. (potentially check on most server actions)
            var lotID = Context.DbId;
            Host.InBackground(() =>
            {
                //TODO: use results in meaningful fashion
                using (var db = DAFactory.Get())
                {
                    bool queryResult;
                    switch (mode)
                    {
                        case 1:
                            queryResult = db.Roommates.Create(new DbRoommate()
                            {
                                avatar_id = avatarID,
                                lot_id = lotID,
                                is_pending = 0,
                                permissions_level = 0
                            });
                            if (queryResult)
                            {
                                vm.ForwardCommand(new VMChangePermissionsCmd()
                                {
                                    TargetUID = avatarID,
                                    Level = VMTSOAvatarPermissions.Roommate,
                                    Verified = true
                                });
                            }
                            break;
                        //the following code enables pending requests, like in the original game. I decided they only really make sense for requests initiated from city.
                        /*
                        case 0:
                            queryResult = db.Roommates.Create(new DbRoommate()
                            {
                                avatar_id = avatarID,
                                lot_id = lotID,
                                is_pending = 1,
                                permissions_level = 0
                            });
                            // dont do anything if we fail. I can see on-lot roommate requests having precedence over off-lot ones though, 
                            // so forcing a wipe of old pending requests might make sense.
                            break;
                        case 1:
                            //accept
                            queryResult = db.Roommates.AcceptRoommateRequest(avatarID, lotID);
                            //if it worked, tell everyone in the lot that there's a new roommate.
                            if (queryResult)
                            {
                                vm.ForwardCommand(new VMChangePermissionsCmd()
                                {
                                    TargetUID = avatarID,
                                    Level = VMTSOAvatarPermissions.Roommate,
                                    Verified = true
                                });
                            }
                            //todo: error feedback. I think the global call is meant to block for an answer and possibly return false.
                            break;
                        case 2:
                            queryResult = db.Roommates.RemoveRoommate(avatarID, lotID) > 0;
                            break;
                            */
                        case 3: //FSO specific mode: switch permissions.
                            queryResult = db.Roommates.UpdatePermissionsLevel(avatarID, lotID, permissions);
                            if (queryResult)
                            {
                                vm.ForwardCommand(new VMChangePermissionsCmd()
                                {
                                    TargetUID = avatarID,
                                    Level = (permissions==0)?VMTSOAvatarPermissions.Roommate:VMTSOAvatarPermissions.BuildBuyRoommate,
                                    Verified = true
                                });
                            }
                            break;
                    }
                    Host.SyncRoommates();
                }
            });
        }

        public void RemoveRoommate(VM vm, VMAvatar avatar)
        {
            var avatarID = avatar.PersistID;
            var lotID = Context.DbId;
            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    var queryResult = db.Roommates.RemoveRoommate(avatarID, lotID) > 0;
                    vm.ForwardCommand(new VMChangePermissionsCmd()
                    {
                        TargetUID = avatarID,
                        Level = VMTSOAvatarPermissions.Visitor,
                        Verified = true
                    });
                    Host.SyncRoommates();
                }
            });
        }

        public void ObtainAvatarFromTicket(VM vm, string ticket, VMAsyncAvatarCallback callback)
        {
            throw new NotImplementedException();
        }

        public void QueueArchitecture(VMNetArchitectureCmd cmd)
        {
            lock (ArchBuffer)
            {
                ArchBuffer.Enqueue(cmd);
            }
        }

        public void LoadPluginPersist(VM vm, uint objectPID, uint pluginID, VMAsyncPluginLoadCallback callback)
        {
            //TODO: maybe a ring backup system for this too? may be more difficult
            Host.InBackground(() =>
            {
                if (objectPID == 0) callback(null);
                try
                {
                    var objStr = objectPID.ToString("x8");
                    var path = Path.Combine(Config.SimNFS, "Objects/" + objStr + "/Plugin/" + pluginID.ToString("x8") + ".dat");

                    //if path does not exist, will throw FileNotFoundException
                    using (var file = File.Open(path, FileMode.Open))
                    {
                        var dat = new byte[file.Length];
                        file.Read(dat, 0, dat.Length);
                        callback(dat);
                    }
                }
                catch (Exception e)
                {
                    //todo: specific types of exception that can be thrown here? instead of just catching em all
                    if (!(e is FileNotFoundException))
                        LOG.Error(e, "Failed to load plugin persist for object " + objectPID.ToString("x8") + " plugin " + pluginID.ToString("x8") + "!");
                    callback(null);
                }
            });
        }

        public void SavePluginPersist(VM vm, uint objectPID, uint pluginID, byte[] data)
        {
            if (objectPID == 0) return; //non-persist objects cannot save persist state!
            Host.InBackground(() =>
            {
                try {
                    var objStr = objectPID.ToString("x8");
                    //make sure this exists
                    Directory.CreateDirectory(Path.Combine(Config.SimNFS, "Objects/" + objStr + "/"));
                    Directory.CreateDirectory(Path.Combine(Config.SimNFS, "Objects/" + objStr + "/Plugin"));

                    var file = File.Open(Path.Combine(Config.SimNFS, "Objects/" + objStr + "/Plugin/" + pluginID.ToString("x8") + ".dat"), FileMode.Create);
                    file.WriteAsync(data, 0, data.Length).ContinueWith(x => file.Close());
                } catch (Exception e)
                {
                    //todo: specific types of exception that can be thrown here? instead of just catching em all
                    LOG.Error(e, "Failed to save plugin persist for object " + objectPID.ToString("x8") + " plugin " + pluginID.ToString("x8") + "!");
                }
            });
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

        public void RegisterNewObject(VM vm, VMEntity obj, VMAsyncPersistIDCallback callback)
        {
            if (obj is VMAvatar) return; //???

            var objid = obj.ObjectID;
            uint guid = obj.Object.OBJ.GUID;
            if (obj.MasterDefinition != null) guid = obj.MasterDefinition.GUID;
            uint? owner = ((VMTSOObjectState)obj.TSOState).OwnerID;
            if (owner == 0) owner = null;
            DbObject dbo = new DbObject()
            {
                owner_id = owner,
                lot_id = Context.DbId,
                shard_id = Context.ShardId,
                dyn_obj_name = "",
                budget = 0,
                graphic = (ushort)obj.GetValue(VMStackObjectVariable.Graphic),
                type = guid,
                value = (uint)obj.MultitileGroup.Price
            };

            Host.InBackground(() =>
            {
                try
                {
                    using (var db = DAFactory.Get())
                    {
                        var id = db.Objects.Create(dbo);
                        if (callback != null) callback(objid, id);
                    }
                }
                catch (Exception) { callback(objid, 0); }
            });
        }

        private DbObject GenerateObjectPersist(VMMultitileGroup obj)
        {
            var bobj = obj.BaseObject;
            uint? owner = ((VMTSOObjectState)obj.BaseObject.TSOState).OwnerID;
            if (owner == 0) owner = null;
            return new DbObject()
            {
                object_id = obj.BaseObject.PersistID,
                owner_id = owner,
                lot_id = Context.DbId,
                dyn_obj_name = obj.Name,
                graphic = (ushort)bobj.GetValue(VMStackObjectVariable.Graphic),
                value = (uint)obj.Price,
                dyn_flags_1 = bobj.DynamicSpriteFlags,
                dyn_flags_2 = bobj.DynamicSpriteFlags2,
                //type and shard id never need to be updated.
            };
        }

        public void ForceInInventory(VM vm, uint objectPID, VMAsyncInventorySaveCallback callback)
        {
            if (objectPID == 0)
            {
                callback(false, objectPID);
                return; //non-persist objects cannot be moved to inventory!
            }
            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    callback(db.Objects.SetInLot(objectPID, null), objectPID);
                }
            });
        }
        private void SaveInventoryState(bool isNew, uint objectPID, VMStandaloneObjectMarshal state, DbObject dbState, uint guid, VMAsyncInventorySaveCallback callback, bool runSync)
        {
            try
            {
                if (isNew)
                {
                    try
                    {
                        using (var db = DAFactory.Get())
                        {
                            dbState.type = guid;
                            dbState.shard_id = Context.ShardId;
                            var id = db.Objects.Create(dbState);
                            dbState.object_id = id;
                            objectPID = id;
                        }
                    }
                    catch (Exception) { callback(false, objectPID); }
                }
                var objStr = objectPID.ToString("x8");
                //make sure this exists
                Directory.CreateDirectory(Path.Combine(Config.SimNFS, "Objects/" + objStr + "/"));
                byte[] data;
                using (var stream = new MemoryStream())
                {
                    var writer = new BinaryWriter(stream);
                    state.SerializeInto(writer);
                    data = stream.ToArray();
                }
                var file = File.Open(Path.Combine(Config.SimNFS, "Objects/" + objStr + "/inventoryState.fsoo"), FileMode.Create);

                if (runSync)
                {
                    file.Write(data, 0, data.Length);
                    using (var db = DAFactory.Get())
                    {
                        //todo: race where inventory object could potentially be placed on the lot before the old instance of it is deleted
                        //probably just block objects with same persist id from being placed.
                        db.Objects.UpdatePersistState(objectPID, dbState);
                        callback(true, objectPID);
                    }
                    file.Close();
                }
                else
                {
                    file.WriteAsync(data, 0, data.Length).ContinueWith((x) =>
                    {
                        using (var db = DAFactory.Get())
                        {
                            db.Objects.UpdatePersistState(objectPID, dbState);
                            callback(true, objectPID);
                        }
                        file.Close();
                    });
                }
            }
            catch (Exception e)
            {
                //todo: specific types of exception that can be thrown here? instead of just catching em all
                LOG.Error(e, "Failed to save inventory state for object " + objectPID.ToString("x8") + "!");
                callback(false, objectPID);
            }
        }


        public void MoveToInventory(VM vm, VMMultitileGroup obj, VMAsyncInventorySaveCallback callback)
        {
            MoveToInventory(vm, obj, callback, false);
        }

        public void MoveToInventory(VM vm, VMMultitileGroup obj, VMAsyncInventorySaveCallback callback, bool runSync)
        {
            var objectPID = obj.BaseObject.PersistID;
            var objb = obj.BaseObject;
            uint guid = objb.Object.OBJ.GUID;
            if (objb.MasterDefinition != null) guid = objb.MasterDefinition.GUID;
            var isNew = objectPID == 0;
            var state = new VMStandaloneObjectMarshal(obj);
            var dbState = GenerateObjectPersist(obj);
            dbState.lot_id = null; //we're removing this object from the lot

            if (runSync)
            {
                SaveInventoryState(isNew, objectPID, state, dbState, guid, callback, true);
            }
            else
            {
                Host.InBackground(() =>
                {
                    SaveInventoryState(isNew, objectPID, state, dbState, guid, callback, false);
                });
            }
        }

        public void UpdateObjectPersist(VM vm, VMMultitileGroup obj, VMAsyncInventorySaveCallback callback)
        {
            var objectPID = obj.BaseObject.PersistID;
            var dbState = GenerateObjectPersist(obj);

            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    //todo: race where inventory object could potentially be placed on the lot before the old instance of it is deleted
                    //probably just block objects with same persist id from being placed.
                    db.Objects.UpdatePersistState(objectPID, dbState);
                    callback(true, objectPID);
                }
            });
        }

        public void PurchaseFromOwner(VM vm, VMMultitileGroup obj, uint purchaserPID, VMAsyncInventorySaveCallback callback, VMAsyncTransactionCallback tcallback)
        {
            var objectPID = obj.BaseObject.PersistID;
            var objb = obj.BaseObject;
            uint guid = objb.Object.OBJ.GUID;
            if (objb.MasterDefinition != null) guid = objb.MasterDefinition.GUID;
            var isNew = objectPID == 0;
            var state = new VMStandaloneObjectMarshal(obj);
            var dbState = GenerateObjectPersist(obj);
            var salePrice = obj.SalePrice;
            var owner = ((VMTSOObjectState)objb.TSOState).OwnerID;
            //object will stay on lot for now.

            Host.InBackground(() =>
            {
                using (var da = DAFactory.Get())
                {
                    SaveInventoryState(isNew, objectPID, state, dbState, guid, (bool success, uint objPID) =>
                    {
                        if (success)
                        {
                            //todo: transaction-ify this whole thing? might need a large scale rollback...
                            var tresult = da.Avatars.Transaction(purchaserPID, owner, salePrice, 0);
                            if (tresult == null) tresult = new Database.DA.Avatars.DbTransactionResult() { success = false };

                            //update the budgets of the respective characters.
                            var finalAmount = salePrice;
                            tcallback(tresult.success, tresult.amount,
                            purchaserPID, (uint)tresult.source_budget,
                            owner, (uint)tresult.dest_budget);

                            if (tresult.success)
                            {
                                dbState.owner_id = purchaserPID;
                                dbState.lot_id = null;
                                da.Objects.UpdatePersistState(objPID, dbState); //perform the final object transfer. todo: logging
                                callback(true, objPID);
                            }
                            else
                            {
                                callback(false, objPID);
                            }

                        }
                        else callback(false, objPID);
                    }, true);
                }
            });
        }

        public void ConsumeInventory(VM vm, uint ownerPID, uint guid, int mode, short num, VMAsyncInventoryConsumeCallback callback)
        {
            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    switch (mode)
                    {
                        case 0:
                            callback(true, db.Objects.ObjOfTypeInAvatarInventory(ownerPID, guid).Count);
                            return;
                        case 1:
                            if (num == 0) callback(true, 0);
                            else
                            {
                                callback(db.Objects.ConsumeObjsOfTypeInAvatarInventory(ownerPID, guid, num), 0);
                                UpdateInventoryFor(vm, ownerPID);
                            }
                            return;
                    }
                }
            });
        }

        public void RetrieveFromInventory(VM vm, uint objectPID, uint ownerPID, bool setOnLot, VMAsyncInventoryRetrieveCallback callback)
        {
            //TODO: maybe a ring backup system for this too? may be more difficult
            Host.InBackground(() =>
            {
                if (objectPID == 0) callback(0, null);
                byte[] dat = null;
                try
                {
                    var objStr = objectPID.ToString("x8");
                    var path = Path.Combine(Config.SimNFS, "Objects/" + objStr + "/inventoryState.fsoo");

                    //if path does not exist, will throw FileNotFoundException
                    using (var file = File.Open(path, FileMode.Open))
                    {
                        dat = new byte[file.Length];
                        file.Read(dat, 0, dat.Length);
                    }
                }
                catch (Exception e)
                {
                    //todo: specific types of exception that can be thrown here? instead of just catching em all
                    if (!(e is FileNotFoundException))
                        LOG.Error(e, "Failed to load inventory state for object " + objectPID.ToString("x8") + "!");
                }

                if (dat != null && dat.Length == 0) dat = null; //treat empty files as if no state were available.
                //put object on this lot
                using (var db = DAFactory.Get())
                {
                    var obj = db.Objects.Get(objectPID);
                    if (obj == null || obj.owner_id != ownerPID) callback(0, null); //object does not exist or request is for wrong owner.
                    if (setOnLot)
                    {
                        if (db.Objects.SetInLot(objectPID, (uint)Context.DbId))
                            callback(obj.type, dat); //load the object with its data, if available.
                        else
                            callback(0, null); //object is already on a lot. we cannot load it!
                    } else if (obj.lot_id == null)
                    {
                        callback(obj.type, dat); //load the object with its data, if available. (for trade) 
                    }
                }
            });
        }

        private void UpdateInventoryFor(VM vm, uint targetPID)
        {
            using (var da = DAFactory.Get())
            {
                var inventory = da.Objects.GetAvatarInventory(targetPID);
                var vmInventory = new List<VMInventoryItem>();
                foreach (var item in inventory)
                {
                    vmInventory.Add(LotContainer.InventoryItemFromDB(item));
                }
                vm.SendDirectCommand(targetPID, new VMNetUpdateInventoryCmd { Items = vmInventory });
            }
        }

        public void DeleteObject(VM vm, uint objectPID, VMAsyncDeleteObjectCallback callback)
        {
            Host.InBackground(() =>
            {
                if (objectPID == 0) callback(true);

                var objStr = objectPID.ToString("x8");
                var path = Path.Combine(Config.SimNFS, "Objects/" + objStr + "/");
                if (Directory.Exists(path)) Directory.Delete(path, true); //delete any persist data we might have, plugins and inventory.

                //remove object from db
                using (var db = DAFactory.Get())
                {
                    callback(db.Objects.Delete(objectPID));
                }
            });
        }

        public void SetSpotlightStatus(VM vm, bool on)
        {
            Host.SetSpotlight(on);
        }

        public void StockOutfit(VM vm, VMGLOutfit outfit, VMAsyncStockOutfitCallback callback)
        {
            Host.InBackground(() => {
                if (outfit.owner_id == 0)
                {
                    callback(false, 0);
                    return;
                }

                using (var db = DAFactory.Get())
                {
                    try {
                        var model = new Database.DA.Outfits.DbOutfit
                        {
                            asset_id = outfit.asset_id,
                            purchase_price = outfit.purchase_price,
                            sale_price = outfit.sale_price,
                            outfit_type = outfit.outfit_type,
                            outfit_source = outfit.outfit_source == VMGLOutfitSource.cas ? Database.DA.Outfits.DbOutfitSource.cas : Database.DA.Outfits.DbOutfitSource.rack
                        };

                        if(outfit.owner_type == VMGLOutfitOwner.AVATAR){
                            model.avatar_owner = outfit.owner_id;
                        }else if(outfit.owner_type == VMGLOutfitOwner.OBJECT){
                            model.object_owner = outfit.owner_id;
                        }

                        var result = db.Outfits.Create(model);
                        callback(result != 0, result);
                    }catch(Exception ex){
                        callback(false, 0);
                    }
                }
            });
        }

        public void GetOutfits(VM vm, VMGLOutfitOwner owner, uint ownerPID, VMAsyncGetOutfitsCallback callback)
        {
            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    var outfits = owner == VMGLOutfitOwner.OBJECT ? db.Outfits.GetByObjectId(ownerPID) : db.Outfits.GetByAvatarId(ownerPID);
                    callback(
                        outfits.Select(x => {
                            var outfit = new VMGLOutfit()
                            {
                                asset_id = x.asset_id,
                                outfit_id = x.outfit_id,
                                sale_price = x.sale_price,
                                purchase_price = x.purchase_price,
                                outfit_type = x.outfit_type,
                                outfit_source = x.outfit_source == Database.DA.Outfits.DbOutfitSource.cas ? VMGLOutfitSource.cas : VMGLOutfitSource.rack
                            };

                            if (x.avatar_owner != null && x.avatar_owner.HasValue)
                            {
                                outfit.owner_type = VMGLOutfitOwner.AVATAR;
                                outfit.owner_id = x.avatar_owner.Value;
                            }else if(x.object_owner != null && x.object_owner.HasValue)
                            {
                                outfit.owner_type = VMGLOutfitOwner.OBJECT;
                                outfit.owner_id = x.object_owner.Value;
                            }

                            return outfit;
                        }).ToArray()
                    );
                }
            });
        }

        public void DeleteOutfit(VM vm, uint outfitPID, VMGLOutfitOwner owner, uint ownerPID, VMAsyncDeleteOutfitCallback callback)
        {
            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    if (owner == VMGLOutfitOwner.OBJECT){
                        callback(db.Outfits.DeleteFromObject(outfitPID, ownerPID));
                    }else if(owner == VMGLOutfitOwner.AVATAR){
                        callback(db.Outfits.DeleteFromAvatar(outfitPID, ownerPID));
                    }
                }
            });
        }

        public void UpdateOutfitSalePrice(VM vm, uint outfitPID, uint objectPID, int newSalePrice, VMAsyncUpdateOutfitSalePriceCallback callback)
        {
            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    callback(db.Outfits.UpdatePrice(outfitPID, objectPID, newSalePrice));   
                }
            });
        }

        public void PurchaseOutfit(VM vm, uint outfitPID, uint objectPID, uint avatarPID, VMAsyncPurchaseOutfitCallback callback)
        {
            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    callback(db.Outfits.ChangeOwner(outfitPID, objectPID, avatarPID));
                }
            });
        }

        public void GetDynPayouts(VMAsyncNewspaperCallback callback)
        {
            Host.InBackground(() =>
            {
                var data = new VMEODFNewspaperData();
                using (var db = DAFactory.Get())
                {
                    var days = (int)(DateTime.UtcNow - new DateTime(1970, 1, 1)).TotalDays;
                    var limitdays = days - 7;
                    var history = db.DynPayouts.GetPayoutHistory(limitdays);

                    var result = new List<VMEODFNewspaperPoint>();
                    foreach (var p in history)
                    {
                        result.Add(new VMEODFNewspaperPoint()
                        {
                            Day = p.day,
                            Skilltype = p.skilltype,
                            Multiplier = p.multiplier,
                            Flags = p.flags
                        });
                    }
                    data.Points = result.OrderBy(x => x.Day).ToList();

                    var evts = db.Events.GetLatestNameDesc(7);
                    data.News = evts.Select(x => new VMEODFNewspaperNews()
                    {
                        ID = x.event_id,
                        Name = x.title,
                        Description = x.description,
                        StartDate = x.start_day.Ticks,
                        EndDate = (x.type == Database.DA.DbEvents.DbEventType.mail_only)?x.start_day.Ticks:x.end_day.Ticks
                    }).ToList();

                    callback(data);
                }
            });
        }

        public void SecureTrade(VM vm, VMEODSecureTradePlayer p1, VMEODSecureTradePlayer p2, VMAsyncSecureTradeCallback callback)
        {
            var moneyMove = p1.MoneyOffer - p2.MoneyOffer;
            Host.InBackground(() =>
            {
                var cityMessages = new List<NotifyLotRoommateChange>();
                using (var db = DAFactory.Get())
                {
                    var failState = VMEODSecureTradeError.SUCCESS;
                    var result = db.Avatars.Transaction(p1.PlayerPersist, p2.PlayerPersist, moneyMove, 9, () =>
                    {
                        //this is all part of the transaction. returning false in here will undo all sql queries we make.

                        //try to transfer the in-inventory objects.
                        VMEODSecureTradeObject lot1 = null;
                        VMEODSecureTradeObject lot2 = null;

                        var objs = new List<uint>();
                        foreach (var item in p1.ObjectOffer)
                        {
                            if (item == null) continue;
                            if (item.LotID > 0)
                            {
                                lot1 = item;
                                continue;
                            }
                            objs.Add(item.PID);
                        }
                        var count = db.Objects.ChangeInventoryOwners(objs, p1.PlayerPersist, p2.PlayerPersist);
                        //if the number of rows changed does not equal the number we wanted to change, the transaction is invalid.
                        if (count != objs.Count) {
                            failState = VMEODSecureTradeError.MISSING_OBJECT;
                            return false;
                        }

                        objs = new List<uint>();
                        foreach (var item in p2.ObjectOffer)
                        {
                            if (item == null) continue;
                            if (item.LotID > 0)
                            {
                                lot2 = item;
                                continue;
                            }
                            objs.Add(item.PID);
                        }
                        count = db.Objects.ChangeInventoryOwners(objs, p2.PlayerPersist, p1.PlayerPersist);
                        if (count != objs.Count) {
                            failState = VMEODSecureTradeError.MISSING_OBJECT;
                            return false;
                        }

                        for (int i = 0; i < 2; i++)
                        {
                            var iLot = (i == 0) ? lot1 : lot2;
                            var myP = (i == 0) ? p1 : p2;
                            var otherP = (i == 0) ? p2 : p1;
                            if (iLot != null)
                            {
                                var lot = db.Lots.Get((int)iLot.LotID);
                                if (lot.owner_id != myP.PlayerPersist && (i == 0 || lot1 == null))
                                {
                                    failState = VMEODSecureTradeError.WRONG_OWNER_LOT;
                                    return false;
                                }
                                if (lot.owner_id != null) db.Roommates.RemoveRoommate(lot.owner_id.Value, lot.lot_id);
                                //evict this roommate from any lots they are on
                                var otherLots = db.Roommates.GetAvatarsLots(otherP.PlayerPersist);
                                foreach (var olot in otherLots)
                                {
                                    db.Roommates.RemoveRoommate(olot.avatar_id, olot.lot_id);
                                    if (olot.permissions_level == 2)
                                    {
                                        if (i == 0 && lot2 != null && lot2.PID == olot.lot_id)
                                        {
                                            //if this lot is being traded later, leave its owner hanging until we can get it again
                                            db.Lots.UpdateOwner(olot.lot_id, null);
                                        }
                                        else
                                        {
                                            db.Lots.ReassignOwner(olot.lot_id);
                                        }
                                    }

                                    //our lot will be changed. update it if we're not giving them our lot (the lot may need to be deleted, and our 
                                    //objects removed, or if we're not giving them the objects on our lot (they must be removed)
                                    //if the lot's open then we need to tell it we're no longer the roommate. 
                                    //we can't detect this from this side, so do it anyways.
                                    cityMessages.Add(new NotifyLotRoommateChange()
                                    {
                                        LotId = olot.lot_id,
                                        AvatarId = olot.avatar_id,
                                        Change = ChangeType.REMOVE_ROOMMATE
                                    });
                                }
                                //create the other avatar as a roommate in this lot, then assign them as owner
                                db.Roommates.CreateOrUpdate(new DbRoommate() { avatar_id = otherP.PlayerPersist, lot_id = lot.lot_id, permissions_level = 2 });
                                db.Lots.UpdateOwner(lot.lot_id, otherP.PlayerPersist);

                                if (iLot.GUID == 2)
                                {
                                    //give them our objects. be extra cautious here and don't transfer the lot if the object count differs.
                                    var objectsTransferred = db.Objects.UpdateObjectOwnerLot(myP.PlayerPersist, lot.lot_id, otherP.PlayerPersist);
                                    if (objectsTransferred != iLot.ObjectCount)
                                    {
                                        failState = VMEODSecureTradeError.MISSING_OBJECT_LOT;
                                        return false;
                                    }
                                }

                                cityMessages.Add(new NotifyLotRoommateChange()
                                {
                                    LotId = lot.lot_id,
                                    AvatarId = otherP.PlayerPersist,
                                    Change = (iLot.GUID == 2) ? ChangeType.BECOME_OWNER_WITH_OBJECTS : ChangeType.BECOME_OWNER,
                                    ReplaceId = (iLot.GUID == 2) ? myP.PlayerPersist : 0
                                });
                            }
                        }

                        return (failState == VMEODSecureTradeError.SUCCESS);
                    });
                    if (failState == VMEODSecureTradeError.SUCCESS && !result.success) failState = VMEODSecureTradeError.MISSING_MONEY;

                    vm.SendCommand(new VMNetAsyncResponseCmd(0, new VMTransferFundsState
                    {
                        Responded = true,
                        Success = result.success,
                        TransferAmount = result.amount,
                        UID1 = p1.PlayerPersist,
                        Budget1 = (uint)result.source_budget,
                        UID2 = p2.PlayerPersist,
                        Budget2 = (uint)result.dest_budget
                    }));
                    //send out updated inventories to both avatars
                    UpdateInventoryFor(vm, p1.PlayerPersist);
                    UpdateInventoryFor(vm, p2.PlayerPersist);
                    callback(failState);

                    if (failState == VMEODSecureTradeError.SUCCESS)
                    {
                        var conn = City.GetByShardId(Context.ShardId);
                        object[] messages = cityMessages.ToArray();
                        conn.Write(messages);
                    }
                }

            });
        }

        public void FindLotAndValue(VM vm, uint persistID, VMAsyncFindLotCallback p)
        {
            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    var lot = db.Lots.GetByOwner(persistID);
                    if (lot == null) p(0, 0, 0, null);

                    var objects = db.Objects.GetByAvatarIdLot(persistID, (uint)lot.lot_id);

                    p((uint)lot.lot_id, objects.Count, objects.Sum(x => x.value), lot.name);
                }
            });
        }
    }
}
