using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Common.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.Objects;
using FSO.Server.Database.DA.Relationships;
using FSO.Server.Database.DA.Roommates;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Servers.City.Domain;
using FSO.SimAntics;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Marshals.Hollow;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Utils;
using Microsoft.Xna.Framework;
using Ninject;
using Ninject.Extensions.ChildKernel;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot.Domain
{
    /// <summary>
    /// 
    /// </summary>
    public class LotContainer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private IDAFactory DAFactory;
        private LotContext Context;
        private ILotHost Host;
        private IKernel Kernel;
        private LotServerConfiguration Config;
        private DbLot LotPersist;
        private List<DbLot> LotAdj;
        private List<DbRoommate> LotRoommates;

        private VM Lot;
        private VMServerDriver VMDriver;
        private LotServerGlobalLink VMGlobalLink;
        private byte[][] HollowLots;
        public int ClientCount = 0;
        public int TimeToShutdown = -1;
        public int LotSaveTicker = 0;
        public int AvatarSaveTicker = 0;

        private bool ShuttingDown;

        private HashSet<uint> AvatarsToSave = new HashSet<uint>();
        private HashSet<IVoltronSession> SessionsToRelease = new HashSet<IVoltronSession>();
        private List<DbRelationship> RelationshipsToSave = new List<DbRelationship>();

        public static readonly int TICKRATE = 30;
        public static readonly int LOT_SAVE_PERIOD = TICKRATE * 60 * 10;
        public static readonly int AVATAR_SAVE_PERIOD = TICKRATE * 60 * 1;

        private IShardRealestateDomain Realestate;
        private VMTSOSurroundingTerrain Terrain;
        private bool JobLot;
        private ManualResetEvent LotActive = new ManualResetEvent(false);
        private Queue<Action> LotThreadActions = new Queue<Action>();
        
        public LotContainer(IDAFactory da, LotContext context, ILotHost host, IKernel kernel, LotServerConfiguration config, IRealestateDomain realestate)
        {
            VM.UseWorld = false;
            DAFactory = da;
            Host = host;
            Context = context;
            Kernel = kernel;
            Config = config;

            JobLot = (context.Id & 0x40000000) > 0;
            if (JobLot) {
                var jobPacked = Context.DbId - 0x200;
                var jobLevel = (short)((jobPacked - 1) & 0xF);
                var jobType = (short)((jobPacked - 1) / 0xF);
                LotPersist = new DbLot
                {
                    lot_id = Context.DbId,
                    location = Context.Id,
                    category = DbLotCategory.money,
                    name = "{job:"+jobType+":"+jobLevel+"}",
                };
                LotAdj = new List<DbLot>();
                LotRoommates = new List<DbRoommate>();
                Terrain = new VMTSOSurroundingTerrain();

                for (int y = 0; y < 3; y++)
                {
                    for (int x = 0; x < 3; x++)
                    {
                        Terrain.Roads[x, y] = 0xF; //crossroads everywhere
                    }
                }
            } else {
                using (var db = DAFactory.Get())
                {
                    LotPersist = db.Lots.Get(context.DbId);
                    LotAdj = db.Lots.GetAdjToLocation(context.ShardId, LotPersist.location);
                    LotRoommates = db.Roommates.GetLotRoommates(context.DbId);
                }
                Realestate = realestate.GetByShard(LotPersist.shard_id);
                GenerateTerrain();
            }
        }

        public void GenerateTerrain()
        {
            Terrain = new VMTSOSurroundingTerrain();
            var coords = MapCoordinates.Unpack(LotPersist.location);
            var map = Realestate.GetMap();
            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    Terrain.BlendN[x, y] = map.GetBlend((coords.X-1)+x, (coords.Y-1)+y);
                }
            }

            for (int y = 0; y < 3; y++)
            {
                for (int x = 0; x < 3; x++)
                {
                    Terrain.Roads[x, y] = map.GetRoad((coords.X - 1) + x, (coords.Y - 1) + y);
                }
            }

            for (int y = 0; y < 4; y++)
            {
                for (int x = 0; x < 4; x++)
                {
                    Terrain.Height[x, y] = map.GetElevation((coords.X - 1) + x, (coords.Y - 1) + y);
                }
            }
        }

        public void LoadAdj()
        {
            LOG.Info("Loading adj lots for lot with dbid = " + Context.DbId);
            HollowLots = new byte[9][];
            var myPos = MapCoordinates.Unpack(LotPersist.location);
            foreach (var lot in LotAdj)
            {
                try
                {
                    var adjLotStr = lot.lot_id.ToString("x8");
                    var path = Path.Combine(Config.SimNFS, "Lots/" + adjLotStr + "/hollow.fsoh");

                    var pos = MapCoordinates.Unpack(lot.location);
                    var x = (pos.X - myPos.X) + 1;
                    var y = (pos.Y - myPos.Y) + 1;
                    if (x < 0 || x > 2 || y < 0 || y > 2) continue; //out of range (why does this happen?)
                    using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        int numBytesToRead = Convert.ToInt32(fs.Length);
                        var file = new byte[(numBytesToRead)];
                        fs.Read(file, 0, numBytesToRead);
                        HollowLots[y * 3 + x] = file;
                    }
                }
                catch (Exception e)
                {
                    LOG.Warn("Failed to load adjacent lot :(");
                    LOG.Warn(e.ToString());
                    //don't bother
                }
            }
        }

        public bool AttemptLoadRing()
        {
            //first let's try load our adjacent lots.
            int attempts = 0;
            var lotStr = LotPersist.lot_id.ToString("x8");

            while (++attempts < Config.RingBufferSize)
            {
                LOG.Info("Checking ring "+attempts+" for lot with dbid = " + Context.DbId);
                try
                {
                    var path = Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/state_"+LotPersist.ring_backup_num.ToString()+".fsov");
                    using (var file = new BinaryReader(File.OpenRead(path)))
                    {
                        var marshal = new VMMarshal();
                        marshal.Deserialize(file);
                        Lot.Load(marshal);
                        CleanLot();
                        Lot.Reset();
                    }

                    using (var db = DAFactory.Get())
                        db.Lots.UpdateRingBackup(LotPersist.lot_id, LotPersist.ring_backup_num);

                    return true;
                }
                catch (Exception e)
                {
                    LotPersist.ring_backup_num--;
                    if (LotPersist.ring_backup_num < 0) LotPersist.ring_backup_num += (sbyte)Config.RingBufferSize;
                }
            }
            
            LOG.Error("FAILED to load all backups for lot with dbid = " + Context.DbId + "! Reverting to empty lot, backing up failed buffers");
            var backupPath = Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/failedRestore" + (DateTime.Now.ToBinary().ToString()) + "/");
            Directory.CreateDirectory(backupPath);
            foreach (var file in Directory.EnumerateFiles(Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/")))
            {
                File.Copy(file, backupPath + Path.GetFileName(file));
            }

            return false;
        }

        public bool SaveRing()
        {
            if (JobLot) return true; //job lots never get saved.
            var newBackup = (sbyte)((LotPersist.ring_backup_num + 1) % Config.RingBufferSize);
            var lotStr = LotPersist.lot_id.ToString("x8");
            Directory.CreateDirectory(Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/"));
            try
            {
                var path = Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/state_" + newBackup.ToString() + ".fsov");
                var marshal = Lot.Save();
                using (var output = new FileStream(path, FileMode.Create))
                {
                    marshal.SerializeInto(new BinaryWriter(output));
                }

                path = Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/hollow.fsoh");
                var hmarshal = Lot.HollowSave();
                using (var output = new FileStream(path, FileMode.Create))
                {
                    hmarshal.SerializeInto(new BinaryWriter(output));
                }

                LotPersist.ring_backup_num = newBackup;
                using (var db = DAFactory.Get())
                    db.Lots.UpdateRingBackup(LotPersist.lot_id, newBackup);
                return true;
            } catch (Exception e)
            {
                LOG.Warn(e, "Failed to save lot with dbid = " + Context.DbId);
                LOG.Warn(e.StackTrace);
                return false;
            }
        }

        private void ReturnInvalidObjects()
        {
            var objectsOnLot = new List<uint>();
            var total = 0;
            var complete = 0;
            var ents = new List<VMEntity>(Lot.Entities);
            foreach (var ent in ents)
            {
                if (ent.PersistID >= 16777216 && ent is VMGameObject)
                {
                    if (ent.MultitileGroup.Objects.Count == 0) continue;
                    objectsOnLot.Add(ent.PersistID);
                    if (!Lot.TSOState.Roommates.Contains(((VMTSOObjectState)ent.TSOState).OwnerID))
                    {
                        //we need to send objects in slots back to their owners inventory too, so we don't lose what was on tables etc.
                        var sendback = new List<VMEntity>();
                        sendback.Add(ent);
                        ObjListAllContained(sendback, ent);

                        foreach (var delE in sendback)
                        {
                            if (delE.MultitileGroup.Objects.Count == 0) continue;
                            if (delE.PersistID >= 16777216 && delE is VMGameObject)
                            {
                                total++;
                                //this is run synchro.
                                VMGlobalLink.MoveToInventory(Lot, delE.MultitileGroup, (success, objid) =>
                                {
                                    Lot.Context.ObjectQueries.RemoveMultitilePersist(Lot, delE.PersistID);
                                    foreach (var o in delE.MultitileGroup.Objects) o.PersistID = 0; //no longer representative of the object in db.
                                    delE.Delete(true, Lot.Context);
                                    complete++;
                                }, true);
                            }
                        }
                    }
                }
            }

            if (objectsOnLot.Count != 0 && !JobLot)
            {
                using (var da = DAFactory.Get())
                {
                    da.Objects.ReturnLostObjects((uint)Context.DbId, objectsOnLot);
                }
            }
        }

        private void ObjListAllContained(List<VMEntity> ents, VMEntity ent)
        {
            for (int i=0; i<ent.TotalSlots(); i++)
            {
                var slotE = ent.GetSlot(i);
                if (slotE != null)
                {
                    ents.Add(slotE);
                    ObjListAllContained(ents, slotE); //recursive
                }
            }
        }

        private void CleanLot()
        {
            var avatars = new List<VMEntity>(Lot.Entities.Where(x => x is VMAvatar && x.PersistID != 0));
            //step 1, force everyone to leave.
            foreach (var avatar in avatars)
                Lot.ForwardCommand(new VMNetSimLeaveCmd()
                {
                    ActorUID = avatar.PersistID,
                    FromNet = false
                });

            //simulate for a bit to try get rid of the avatars on the lot
            try
            {
                for (int i = 0; i < 30 * TICKRATE && Lot.Entities.FirstOrDefault(x => x is VMAvatar && x.PersistID > 0) != null; i++)
                {
                    if (i == 30 * TICKRATE - 1) LOG.Warn("Failed to clean lot with dbid = " + Context.DbId);
                    Lot.Tick();
                }
            }
            catch (Exception) { } //if something bad happens just immediately try to delete everyone

            avatars = new List<VMEntity>(Lot.Entities.Where(x => x is VMAvatar && (x.PersistID != 0 || (!(x as VMAvatar).IsPet))));
            foreach (var avatar in avatars) avatar.Delete(true, Lot.Context);
        }


        public void ResetVM()
        {
            LOG.Info("Resetting VM for lot with dbid = " + Context.DbId);
            VMGlobalLink = Kernel.Get<LotServerGlobalLink>();
            VMDriver = new VMServerDriver(VMGlobalLink);
            VMDriver.OnTickBroadcast += TickBroadcast;
            VMDriver.OnDirectMessage += DirectMessage;
            VMDriver.OnDropClient += DropClient;

            var vm = new VM(new VMContext(null), VMDriver, new VMNullHeadlineProvider());
            Lot = vm;
            vm.Init();

            bool isNew = false;
            LoadAdj();
            if (!JobLot && LotPersist.ring_backup_num > -1 && AttemptLoadRing())
            {
                
            }
            else
            {
                var path = "Content/Blueprints/empty_lot_fso.xml";

                var floorClip = Rectangle.Empty;
                var offset = new Point();
                var targetSize = 0;

                short jobLevel = -1;
                if (JobLot)
                {
                    //non-road tiles start at (8,8), end at (56,56)
                    //offset (7,14)

                    floorClip = new Rectangle(8, 8, 56 - 8, 56 - 8);
                    offset = new Point(7, 14);
                    targetSize = 77;

                    var jobPacked = Context.DbId - 0x200;
                    jobLevel = (short)((jobPacked - 1)&0xF);
                    var jobType = (short)((jobPacked - 1)/0xF);
                    var randomChance = (jobType > 2 && jobLevel > 6) ? 2:1;
                    path = Content.Content.Get().GetPath("housedata/blueprints/" + JobMatchmaker.JobXMLName[jobType]
                        + JobMatchmaker.JobGradeToLotGroup[jobType][jobLevel].ToString().PadLeft(2, '0') + "_"
                        + (new Random()).Next(randomChance).ToString().PadLeft(2, '0')
                        + ".xml");
                }
                vm.SendCommand(new VMBlueprintRestoreCmd
                {
                    JobLevel = jobLevel,
                    XMLData = File.ReadAllBytes(path),

                    FloorClipX = floorClip.X,
                    FloorClipY = floorClip.Y,
                    FloorClipWidth = floorClip.Width,
                    FloorClipHeight = floorClip.Height,
                    OffsetX=offset.X,
                    OffsetY=offset.Y,
                    TargetSize=targetSize
                });
                vm.Update();
                vm.Update();

                isNew = true;
                SaveRing();
            }

            vm.TSOState.Terrain = Terrain;
            vm.TSOState.Name = LotPersist.name;
            vm.TSOState.OwnerID = LotPersist.owner_id;
            vm.TSOState.Roommates = new HashSet<uint>();
            vm.TSOState.BuildRoommates = new HashSet<uint>();
            vm.TSOState.PropertyCategory = (byte)LotPersist.category;
            foreach (var roomie in LotRoommates)
            {
                if (roomie.is_pending > 0) continue;
                vm.TSOState.Roommates.Add(roomie.avatar_id);
                if (roomie.permissions_level > 0)
                    vm.TSOState.BuildRoommates.Add(roomie.avatar_id);
                if (roomie.permissions_level > 1)
                    vm.TSOState.OwnerID = roomie.avatar_id;
            }

            var time = DateTime.UtcNow;
            var tsoTime = TSOTime.FromUTC(time);

            vm.Context.Clock.Hours = tsoTime.Item1;
            vm.Context.Clock.Minutes = tsoTime.Item2;

            VMLotTerrainRestoreTools.RestoreTerrain(vm);
            if (isNew) VMLotTerrainRestoreTools.PopulateBlankTerrain(vm);

            vm.Context.UpdateTSOBuildableArea();

            vm.MyUID = uint.MaxValue - 1;
            ReturnInvalidObjects();

            var entClone = new List<VMEntity>(vm.Entities);
            foreach (var ent in entClone)
            {
                if (ent is VMGameObject)
                {
                    ((VMGameObject)ent).Disabled &= ~VMGameObjectDisableFlags.TransactionIncomplete;
                    ((VMGameObject)ent).DisableIfTSOCategoryWrong(vm.Context);
                    if (ent.GetFlag(VMEntityFlags.Occupied))
                    {
                        ent.ResetData();
                        ent.Init(vm.Context); //objects should not be occupied when we join the lot...
                    }
                    {
                        ent.ExecuteEntryPoint(2, vm.Context, true);
                    }
                }
            }
            LotActive.Set();
        }

        private void DropClient(VMNetClient target)
        {
            //The VM wants us to drop this client.
            //...uh, tell the host because we don't control the voltron sessions
            Host.DropClient(target.PersistID);
        }

        public void Message(IVoltronSession session, FSOVMCommand cmd)
        {
            VMDriver.SubmitMessage(session.AvatarId, new VMNetMessage(VMNetMessageType.Command, cmd.Data));
        }

        private void DirectMessage(VMNetClient target, VMNetMessage msg)
        {
            object packet = (msg.Type == VMNetMessageType.Direct) ?
                (object)(new FSOVMDirectToClient() { Data = msg.Data })
                : (object)(new FSOVMTickBroadcast() { Data = msg.Data });
            Host.Send(target.PersistID, packet);
        }

        private void TickBroadcast(VMNetMessage msg, HashSet<VMNetClient> ignore)
        {
            HashSet<uint> ignoreIDs = new HashSet<uint>(ignore.Select(x => x.PersistID));
            Host.Broadcast(ignoreIDs, new FSOVMTickBroadcast() { Data = msg.Data });
        }

        /// <summary>
        /// Load and initialize everything to start up the lot
        /// </summary>
        public void Run()
        {
            try
            {
                ResetVM();
            } catch (Exception e)
            {
                LOG.Info("LOT " + Context.DbId + " LOAD EXECPTION:" + e.ToString());
                Host.Shutdown();
                return;
            }
            LOG.Info("Starting to host lot with dbid = " + Context.DbId);
            Host.SetOnline(true);

            var timeKeeper = new Stopwatch(); //todo: smarter timing
            timeKeeper.Start();
            long lastTick = 0;

            LotSaveTicker = LOT_SAVE_PERIOD;
            AvatarSaveTicker = AVATAR_SAVE_PERIOD;
            while (true)
            {
                bool noRemainingUsers = ClientCount == 0;
                lastTick++;
                //sometimes avatars can be killed immediately after their kill timer starts (this frame will run the leave lot interaction)
                //this works around that possibility. 
                var preTickAvatars = Lot.Context.ObjectQueries.Avatars.Select(x => (VMAvatar)x).ToList();
                try
                {
                    Lot.Tick();
                }
                catch (Exception e)
                {
                    //something bad happened. not entirely sure how we should deal with this yet
                    LOG.Error("VM ERROR: "+e.StackTrace);
                    Host.Shutdown();
                    return;
                }

                if (noRemainingUsers)
                {
                    if (TimeToShutdown == -1)
                        TimeToShutdown = TICKRATE * 20; //lot shuts down 20 seconds after everyone leaves
                    else {
                        if (--TimeToShutdown == 0 || (ShuttingDown && TimeToShutdown < (TICKRATE * 20 - 10)))
                        {
                            Shutdown();
                            return; //kill the lot
                        }
                    }
                }
                else if (TimeToShutdown != -1)
                    TimeToShutdown = -1;

                if (--LotSaveTicker <= 0)
                {
                    SaveRing();
                    LotSaveTicker = LOT_SAVE_PERIOD;
                }

                var beingKilled = preTickAvatars.Where(x => x.KillTimeout == 1);
                if (beingKilled.Count() > 0)
                {
                    //avatars that are being killed could die before their user disconnects. It's important to save them immediately.
                    LOG.Info("Avatar Kill Save");
                    SaveAvatars(beingKilled, true);
                }

                foreach (var avatar in Lot.Context.ObjectQueries.AvatarsByPersist)
                {
                    if (avatar.Value.KillTimeout == 1)
                    {
                        //this avatar has begun being killed. Save them immediately.
                        SaveAvatar(avatar.Value);
                    }
                }

                if (--AvatarSaveTicker <= 0)
                {
                    //save all avatars
                    SaveAvatars(Lot.Context.ObjectQueries.Avatars.Cast<VMAvatar>(), false);
                    AvatarSaveTicker = AVATAR_SAVE_PERIOD;
                }

                lock (SessionsToRelease)
                {
                    //save avatar state, then release their avatar claims afterwards.
                    //SaveAvatars(SessionsToRelease.Select(x => Lot.GetAvatarByPersist(x.AvatarId)), true); //todo: is this performed by the fact that we started the persist save above?
                    foreach (var session in SessionsToRelease)
                    {
                        LOG.Info("Avatar Session Release");
                        Host.ReleaseAvatarClaim(session);
                    }
                    SessionsToRelease.Clear();
                }

                lock (LotThreadActions)
                {
                    while (LotThreadActions.Count > 0)
                    {
                        LotThreadActions.Dequeue()();
                    }
                }

                Thread.Sleep((int)Math.Max(0, (((lastTick + 1)*1000)/TICKRATE) - timeKeeper.ElapsedMilliseconds));
            }
        }

        public void BlockOnLotThread(Action action)
        {
            var evt = new AutoResetEvent(false);
            lock (LotThreadActions)
            {
                LotThreadActions.Enqueue(() =>
                {
                    action();
                    evt.Set();
                });
            }
            evt.WaitOne();
        }

        public bool IsAvatarOnLot(uint pid)
        {
            //we need to check if the avatar's sim is still on the lot. their data + claim might have left, but the avatar could still be here.
            bool result = false;
            LotActive.WaitOne(); //wait til we're active at least
            BlockOnLotThread(() =>
            {
                if (Lot != null) result = Lot.Context.ObjectQueries.AvatarsByPersist.ContainsKey(pid);
            });
            return result;
        }

        public void SaveAvatars(IEnumerable<VMAvatar> avatars, bool ignoreKill)
        {
            RelationshipsToSave.Clear();
            foreach (var avatar in avatars)
            {
                if (avatar != null && avatar.PersistID != 0 && (ignoreKill || avatar.KillTimeout == -1)) SaveAvatar(avatar);
            }
            if (RelationshipsToSave.Count > 0) BatchRelationshipSave();
        }

        //Run on the background thread
        public void AvatarJoin(IVoltronSession session)
        {
            LotActive.WaitOne(); //wait til we're active at least
            using (var da = DAFactory.Get())
            {
                ClientCount++;
                var avatar = da.Avatars.Get(session.AvatarId);
                var rels = da.Relationships.GetOutgoing(session.AvatarId);
                var jobinfo = da.Avatars.GetJobLevels(session.AvatarId);
                var inventory = da.Objects.GetAvatarInventory(session.AvatarId);
                var myRoomieLots = da.Roommates.GetAvatarsLots(session.AvatarId); //might want to use other entries to update the roomies table entirely.
                var myIgnored = da.Bookmarks.GetAvatarIgnore(session.AvatarId);
                LOG.Info("Avatar " + avatar.name + " has joined");

                //Load all the avatars data
                var state = StateFromDB(avatar, rels, jobinfo, myRoomieLots, myIgnored);

                var client = new VMNetClient();
                client.AvatarState = state;
                client.RemoteIP = session.IpAddress;
                client.PersistID = session.AvatarId;

                if (TimeToShutdown == 0)
                {
                    //oops... bad bad bad
                    DropClient(client);
                    return;
                }
                VMDriver.ConnectClient(client);
                VMDriver.SendDirectCommand(client, new VMNetAdjHollowSyncCmd { HollowAdj = HollowLots });

                var vmInventory = new List<VMInventoryItem>();
                foreach (var item in inventory)
                {
                    vmInventory.Add(InventoryItemFromDB(item));
                }
                VMDriver.SendDirectCommand(client, new VMNetUpdateInventoryCmd { Items = vmInventory });
            }
        }

        public static VMInventoryItem InventoryItemFromDB(DbObject obj)
        {
            return new VMInventoryItem
            {
                ObjectPID = obj.object_id,
                GUID = obj.type,
                Name = obj.dyn_obj_name,
                Value = obj.value,
                DynFlags1 = obj.dyn_flags_1,
                DynFlags2 = obj.dyn_flags_2,
                Graphic = obj.graphic
            };
        }
        
        public void BatchRelationshipSave()
        {
            var saves = RelationshipsToSave;
            RelationshipsToSave = new List<DbRelationship>();
            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    db.Relationships.UpdateMany(saves);
                }
            });
        }

        public void SaveAvatar(VMAvatar avatar)
        {
            var statevm = new VMNetAvatarPersistState();
            statevm.Save(avatar);
            foreach (var relsID in avatar.ChangedRels)
            {
                int i = 0;
                if (!avatar.MeToPersist.ContainsKey(relsID)) continue;
                var rels = avatar.MeToPersist[relsID];
                foreach (var value in rels)
                {
                    RelationshipsToSave.Add(new DbRelationship
                    {
                        from_id = avatar.PersistID,
                        to_id = relsID,
                        index = (uint)(i++),
                        value = (int)value
                    });
                }
            }
            avatar.ChangedRels.Clear();
            var dbState = StateToDb(statevm);
            dbState.avatar_id = avatar.PersistID;
            var pid = avatar.PersistID;

            DbJobLevel jobLevel = null;
            if (dbState.current_job > 0)
            {
                VMTSOJobInfo info = null;
                ((VMTSOAvatarState)avatar.TSOState).JobInfo.TryGetValue((short)dbState.current_job, out info);
                if (info != null)
                {
                    jobLevel = new DbJobLevel()
                    {
                        avatar_id = pid,
                        job_type = dbState.current_job,
                        job_experience = (ushort)info.Experience,
                        job_level = (ushort)info.Level,
                        job_sickdays = (ushort)info.SickDays,
                        job_statusflags = (ushort)info.StatusFlags
                    };
                }
            }
            
            Host.InBackground(() =>
            {
                using (var db = DAFactory.Get())
                {
                    db.Avatars.UpdateAvatarLotSave(pid, dbState);
                    if (jobLevel != null) db.Avatars.UpdateAvatarJobLevel(jobLevel);
                }
            });
        }

        private VMNetAvatarPersistState StateFromDB(DbAvatar avatar, List<DbRelationship> rels, List<DbJobLevel> jobs, List<DbRoommate> myRoomieLots, List<uint> ignored)
        {
            var state = new VMNetAvatarPersistState();
            state.Name = avatar.name;
            state.PersistID = avatar.avatar_id;
            state.DefaultSuits = new SimAntics.VMAvatarDefaultSuits(avatar.gender == DbAvatarGender.female);
            state.DefaultSuits.Daywear = avatar.body;
            state.DefaultSuits.Swimwear = avatar.body_swimwear;
            state.DefaultSuits.Sleepwear = avatar.body_sleepwear;
            state.BodyOutfit = (avatar.body_current == 0)?avatar.body:avatar.body_current;
            state.HeadOutfit = avatar.head;
            state.Gender = (short)avatar.gender;
            state.Budget = (uint)avatar.budget;
            state.SkinTone = avatar.skin_tone;

            var now = Epoch.Now;
            var age = (uint)((now - avatar.date) / ((long)60 * 60 * 24));

            state.SkillLock = (short)(20 + age / 7);
            state.SkillLockBody = (short)(avatar.lock_body*100);
            state.SkillLockCharisma = (short)(avatar.lock_charisma * 100);
            state.SkillLockCooking = (short)(avatar.lock_cooking * 100);
            state.SkillLockCreativity = (short)(avatar.lock_creativity * 100);
            state.SkillLockLogic = (short)(avatar.lock_logic * 100);
            state.SkillLockMechanical = (short)(avatar.lock_mechanical * 100);

            state.BodySkill = (short)avatar.skill_body;
            state.CharismaSkill = (short)avatar.skill_charisma;
            state.CookingSkill = (short)avatar.skill_cooking;
            state.CreativitySkill = (short)avatar.skill_creativity;
            state.LogicSkill = (short)avatar.skill_logic;
            state.MechanicalSkill = (short)avatar.skill_mechanical;

            state.IsGhost = (short)avatar.is_ghost;

            state.DeathTicker = (short)avatar.ticker_death;
            state.GardenerRehireTicker = (short)avatar.ticker_gardener;
            state.MaidRehireTicker = (short)avatar.ticker_maid;
            state.RepairmanRehireTicker = (short)avatar.ticker_repairman;

            state.OnlineJobID = (short)avatar.current_job;
            state.IgnoredAvatars = new HashSet<uint>(ignored);
            foreach (var job in jobs)
            {
                state.OnlineJobInfo[(short)job.job_type] = new VMTSOJobInfo()
                {
                    Experience = (short)job.job_experience,
                    Level = (short)job.job_level,
                    SickDays = (short)job.job_sickdays,
                    StatusFlags = (short)job.job_statusflags
                };
            }

            if (myRoomieLots.Count == 0)
                state.AvatarFlags |= VMTSOAvatarFlags.CanBeRoommate; //we're not roommate anywhere, so we can be here.
            var roomieStatus = myRoomieLots.FindAll(x => x.lot_id == Context.DbId).FirstOrDefault();
            if (roomieStatus != null && roomieStatus.is_pending == 0)
            {
                switch (roomieStatus.permissions_level)
                {
                    case 0:
                        state.Permissions = VMTSOAvatarPermissions.Roommate; break;
                    case 1:
                        state.Permissions = VMTSOAvatarPermissions.BuildBuyRoommate; break;
                    case 2:
                        state.Permissions = VMTSOAvatarPermissions.Owner; break;
                }
            }
            else state.Permissions = VMTSOAvatarPermissions.Visitor;

            if (avatar.moderation_level > 0) state.Permissions = VMTSOAvatarPermissions.Admin;

            var motives = new short[16];
            for (int i=0; i<16; i++)
            {
                var twoi = i + i;
                motives[i] = (short)((avatar.motive_data[twoi]<<8) | avatar.motive_data[twoi + 1]);
            }
            state.MotiveData = motives;

            var relDict = new Dictionary<uint, List<int>>();
            foreach (var rel in rels)
            {
                if (!relDict.ContainsKey(rel.to_id)) relDict[rel.to_id] = new List<int>();
                var list = relDict[rel.to_id];
                while (list.Count <= rel.index) list.Add(0);
                list[(int)rel.index] = rel.value;
            }

            state.Relationships = new VMEntityPersistRelationshipMarshal[relDict.Count];
            for (int i=0; i<relDict.Count; i++)
            {
                var dictItem = relDict.ElementAt(i);
                var marshal = new VMEntityPersistRelationshipMarshal();
                marshal.Target = dictItem.Key;
                marshal.Values = dictItem.Value.ConvertAll(x => (short)x).ToArray();
                state.Relationships[i] = marshal;
            }

            return state;
        }

        public DbAvatar StateToDb(VMNetAvatarPersistState avatar)
        {
            var state = new DbAvatar();
            state.body = avatar.DefaultSuits.Daywear;
            state.body_sleepwear = avatar.DefaultSuits.Sleepwear;
            state.body_swimwear = avatar.DefaultSuits.Swimwear;
            state.body_current = avatar.BodyOutfit;

            state.skilllock = (byte)avatar.SkillLock;
            state.lock_body = (ushort)(avatar.SkillLockBody / 100);
            state.lock_charisma = (ushort)(avatar.SkillLockCharisma / 100);
            state.lock_cooking = (ushort)(avatar.SkillLockCooking / 100);
            state.lock_creativity = (ushort)(avatar.SkillLockCreativity / 100);
            state.lock_logic = (ushort)(avatar.SkillLockLogic / 100);
            state.lock_mechanical = (ushort)(avatar.SkillLockMechanical / 100);

            state.skill_body = (ushort)avatar.BodySkill;
            state.skill_charisma = (ushort)avatar.CharismaSkill;
            state.skill_cooking = (ushort)avatar.CookingSkill;
            state.skill_creativity = (ushort)avatar.CreativitySkill;
            state.skill_logic = (ushort)avatar.LogicSkill;
            state.skill_mechanical = (ushort)avatar.MechanicalSkill;

            state.is_ghost = (ushort)avatar.IsGhost;

            state.ticker_death = (ushort)avatar.DeathTicker;
            state.ticker_gardener = (ushort)avatar.GardenerRehireTicker;
            state.ticker_maid = (ushort)avatar.MaidRehireTicker;
            state.ticker_repairman = (ushort)avatar.RepairmanRehireTicker;

            state.current_job = (ushort)avatar.OnlineJobID;

            var motives = new byte[32];
            for (int i = 0; i < 16; i++)
            {
                var twoi = i + i;
                motives[twoi] = (byte)(avatar.MotiveData[i] >> 8);
                motives[twoi + 1] = (byte)avatar.MotiveData[i];
            }
            state.motive_data = motives;

            return state;
        }

        public void ForceShutdown()
        {
            //this lot needs to be shutdown asap. As soon as all avatars are disconnected/saved, clean lot and shutdown.
            ShuttingDown = true;
        }

        public void Shutdown()
        {
            //shut down this lot. Do a final save and close everything down.
            LOG.Info("Lot with dbid = " + Context.DbId + " shutting down.");
            try
            {
                ReturnInvalidObjects();
            }
            catch (Exception e) { }
            SaveRing();
            Host.Shutdown();
        }

        //Run on the background thread
        public void AvatarLeave(IVoltronSession session)
        {
            //Exit lot, Persist the avatars data, remove avatar lock
            LOG.Info("Avatar left");

            // defer the following so that the avatar save is queued, then their session's claim is released.
            lock (SessionsToRelease) SessionsToRelease.Add(session);

            VMDriver.DisconnectClient(session.AvatarId);
            ClientCount--;
        }

    }
}
