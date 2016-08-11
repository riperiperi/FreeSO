using FSO.Server.Database.DA;
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
        private bool WaitingOnArch;

        public LotServerGlobalLink(LotServerConfiguration config, IDAFactory da, LotContext context, ILotHost host)
        {
            DAFactory = da;
            Host = host;
            Context = context;
            Config = config;
        }

        public void LeaveLot(VM vm, VMAvatar avatar)
        {
            avatar.Delete(true, vm.Context);
            vm.Context.VM.SignalChatEvent(new VMChatEvent(avatar.PersistID, VMChatEventType.Leave, avatar.Name));
        }

        public void PerformTransaction(VM vm, bool testOnly, uint uid1, uint uid2, int amount, VMAsyncTransactionCallback callback)
        {

            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    var result = (testOnly)?db.Avatars.TestTransaction(uid1, uid2, amount, 0):db.Avatars.Transaction(uid1, uid2, amount, 0);
                    if (result == null) result = new Database.DA.Avatars.DbTransactionResult() { success = false };

                    var finalAmount = amount;
                    callback(result.success, result.amount,
                    uid1, (uint)result.source_budget,
                    uid2, (uint)result.dest_budget);
                }
            });
        }

        public void RequestRoommate(VM vm, VMAvatar avatar)
        {
            throw new NotImplementedException();
        }

        public void RemoveRoommate(VM vm, VMAvatar avatar)
        {
            throw new NotImplementedException();
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

        public void RegisterNewObject(VM vm, VMEntity obj, VMAsyncPersistIDCallback callback)
        {
            if (obj is VMAvatar) return; //???

            var objid = obj.ObjectID;
            uint guid = obj.Object.OBJ.GUID;
            if (obj.MasterDefinition != null) guid = obj.MasterDefinition.GUID;
            DbObject dbo = new DbObject()
            {
                owner_id = ((VMTSOObjectState)obj.TSOState).OwnerID,
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
                catch (Exception e) { callback(objid, 0); }
            });
        }

        private DbObject GenerateObjectPersist(VMMultitileGroup obj)
        {
            var bobj = obj.BaseObject;
            return new DbObject()
            {
                object_id = obj.BaseObject.PersistID,
                owner_id = ((VMTSOObjectState)obj.BaseObject.TSOState).OwnerID,
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
                callback(false);
                return; //non-persist objects cannot be moved to inventory!
            }
            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    callback(db.Objects.SetInLot(objectPID, null));
                }
            });
        }

        public void MoveToInventory(VM vm, VMMultitileGroup obj, VMAsyncInventorySaveCallback callback)
        {
            var objectPID = obj.BaseObject.PersistID;
            if (objectPID == 0)
            {
                callback(false);
                return; //non-persist objects cannot be moved to inventory!
            }
            var state = new VMStandaloneObjectMarshal(obj);
            var dbState = GenerateObjectPersist(obj);
            dbState.lot_id = null; //we're removing this object from the lot
            Host.InBackground(() =>
            {
                try
                {
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
                    file.WriteAsync(data, 0, data.Length).ContinueWith((x) =>
                    {
                        using (var db = DAFactory.Get())
                        {
                            callback(db.Objects.UpdatePersistState(objectPID, dbState)); //if object is on another lot or does not exist, this will fail.
                        }
                        file.Close();
                    });
                }
                catch (Exception e)
                {
                    //todo: specific types of exception that can be thrown here? instead of just catching em all
                    LOG.Error(e, "Failed to save inventory state for object " + objectPID.ToString("x8") + "!");
                    callback(false);
                }
            });
        }

        public void RetrieveFromInventory(VM vm, uint objectPID, uint ownerPID, VMAsyncInventoryRetrieveCallback callback)
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
                    if (db.Objects.SetInLot(objectPID, (uint)Context.DbId))
                        callback(obj.type, dat); //load the object with its data, if available.
                    else
                        callback(0, null); //object is already on a lot. we cannot load it!
                }
            });
        }

        public void DeleteObject(VM vm, uint objectPID, uint value, VMAsyncDeleteObjectCallback callback)
        {
            throw new NotImplementedException();
        }
    }
}
