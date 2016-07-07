using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.Relationships;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
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

        private VM Lot;
        private VMServerDriver VMDriver;
        private LotServerGlobalLink VMGlobalLink;
        private byte[][] HollowLots;
        public int ClientCount = 0;
        public int TimeToShutdown = -1;
        public int LotSaveTicker = 0;
        public int AvatarSaveTicker = 0;
        private HashSet<uint> AvatarsToSave = new HashSet<uint>();
        private List<DbRelationship> RelationshipsToSave = new List<DbRelationship>();

        public static readonly int LOT_SAVE_PERIOD = 60 * 60 * 10;
        public static readonly int AVATAR_SAVE_PERIOD = 60 * 60 * 1;

        private IShardRealestateDomain Realestate;
        private VMTSOSurroundingTerrain Terrain;
        
        public LotContainer(IDAFactory da, LotContext context, ILotHost host, IKernel kernel, LotServerConfiguration config, IRealestateDomain realestate)
        {
            VM.UseWorld = false;
            DAFactory = da;
            Host = host;
            Context = context;
            Kernel = kernel;
            Config = config;

            using (var db = DAFactory.Get())
            {
                LotPersist = db.Lots.Get(context.DbId);
                LotAdj = db.Lots.GetAdjToLocation(context.ShardId, LotPersist.location);
            }
            Realestate = realestate.GetByShard(LotPersist.shard_id);

            GenerateTerrain();
            ResetVM();
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

        public bool AttemptLoadRing()
        {
            //first let's try load our adjacent lots.
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
                    using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        int numBytesToRead = Convert.ToInt32(fs.Length);
                        var file = new byte[(numBytesToRead)];
                        fs.Read(file, 0, numBytesToRead);
                        HollowLots[y * 3 + x] = file;
                    }
                } catch (Exception e)
                {
                    LOG.Warn("Failed to load adjacent lot :(");
                    LOG.Warn(e.StackTrace);
                    //don't bother
                }
            }

            int attempts = 0;
            var lotStr = LotPersist.lot_id.ToString("x8");
            while (++attempts < Config.RingBufferSize)
            {
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
                for (int i = 0; i < 30 * 60 && Lot.Entities.FirstOrDefault(x => x is VMAvatar && x.PersistID > 0) != null; i++)
                {
                    if (i == 30 * 60 - 1) LOG.Warn("Failed to clean lot with dbid = " + Context.DbId);
                    Lot.Update();
                }
            }
            catch (Exception) { } //if something bad happens just immediately try to delete everyone

            avatars = new List<VMEntity>(Lot.Entities.Where(x => x is VMAvatar && (x.PersistID != 0 || (!(x as VMAvatar).IsPet))));
            foreach (var avatar in avatars) avatar.Delete(true, Lot.Context);
        }


        public void ResetVM()
        {
            VMGlobalLink = Kernel.Get<LotServerGlobalLink>();
            VMDriver = new VMServerDriver(VMGlobalLink);
            VMDriver.OnTickBroadcast += TickBroadcast;
            VMDriver.OnDirectMessage += DirectMessage;
            VMDriver.OnDropClient += DropClient;

            var vm = new VM(new VMContext(null), VMDriver, new VMNullHeadlineProvider());
            Lot = vm;
            vm.Init();

            bool isNew = false;
            if (LotPersist.ring_backup_num > -1 && AttemptLoadRing())
            {
                
            }
            else
            {
                var path = "Content/Blueprints/empty_lot_fso.xml";
                string filename = Path.GetFileName(path);

                short jobLevel = -1;

                //quick hack to find the job level from the chosen blueprint
                //the final server will know this from the fact that it wants to create a job lot in the first place...

                try
                {
                    if (filename.StartsWith("nightclub") || filename.StartsWith("restaurant") || filename.StartsWith("robotfactory"))
                        jobLevel = Convert.ToInt16(filename.Substring(filename.Length - 9, 2));
                }
                catch (Exception) { }
                vm.SendCommand(new VMBlueprintRestoreCmd
                {
                    JobLevel = jobLevel,
                    XMLData = File.ReadAllBytes(path)
                });
                vm.Update();
                vm.Update();

                isNew = true;
                SaveRing();
            }

            vm.TSOState.Terrain = Terrain;
            vm.TSOState.Name = LotPersist.name;
            vm.TSOState.OwnerID = LotPersist.owner_id;

            var time = DateTime.UtcNow;
            var cycle = (time.Hour % 2 == 1) ? 3600 : 0;
            cycle += time.Minute * 60 + time.Second;

            vm.Context.Clock.Hours = cycle / 300;
            vm.Context.Clock.Minutes = (cycle % 300) / 5;

            VMLotTerrainRestoreTools.RestoreTerrain(vm);
            if (isNew) VMLotTerrainRestoreTools.PopulateBlankTerrain(vm);

            vm.MyUID = uint.MaxValue - 1;
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
            LOG.Info("Starting to host lot with dbid = " + Context.DbId);
            Host.SetOnline(true);

            var timeKeeper = new Stopwatch(); //todo: smarter timing
            timeKeeper.Start();
            long lastMs = 0;

            LotSaveTicker = LOT_SAVE_PERIOD;
            AvatarSaveTicker = AVATAR_SAVE_PERIOD;
            while (true)
            {
                lastMs += 16;
                try
                {
                    Lot.Update();
                }
                catch (Exception e)
                {
                    //something bad happened. not entirely sure how we should deal with this yet
                }

                if (ClientCount == 0)
                {
                    if (TimeToShutdown == -1)
                        TimeToShutdown = 60 * 20; //lot shuts down 20 seconds after everyone leaves
                    else {
                        if (--TimeToShutdown == 0)
                        {
                            Shutdown();
                            break; //kill the lot
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

                if (--AvatarSaveTicker <= 0)
                {
                    //save all avatars
                    RelationshipsToSave.Clear();
                    foreach (var avatar in Lot.Context.SetToNextCache.Avatars)
                    {
                        if (avatar.PersistID != 0) SaveAvatar((VMAvatar)avatar);
                    }
                    if (RelationshipsToSave.Count > 0) BatchRelationshipSave();
                    AvatarSaveTicker = AVATAR_SAVE_PERIOD;
                }

                lock (AvatarsToSave)
                {
                    RelationshipsToSave.Clear();

                    foreach (var pid in AvatarsToSave)
                    {
                        var avatar = Lot.GetObjectByPersist(pid);
                        if (avatar == null || avatar is VMGameObject) continue;
                        SaveAvatar((VMAvatar)avatar);
                    }
                    if (RelationshipsToSave.Count > 0) BatchRelationshipSave();
                    AvatarsToSave.Clear();
                }

                Thread.Sleep((int)Math.Max(0, (lastMs + 16) - timeKeeper.ElapsedMilliseconds));
            }
        }

        //Run on the background thread
        public void AvatarJoin(IVoltronSession session)
        {
            using (var da = DAFactory.Get())
            {
                ClientCount++;
                var avatar = da.Avatars.Get(session.AvatarId);
                var rels = da.Relationships.GetOutgoing(session.AvatarId);
                var jobinfo = da.Avatars.GetJobLevels(session.AvatarId);
                LOG.Info("Avatar " + avatar.name + " has joined");

                //Load all the avatars data
                var state = StateFromDB(avatar, rels, jobinfo);
                state.Permissions = SimAntics.Model.TSOPlatform.VMTSOAvatarPermissions.Owner;

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
                VMDriver.SendOneOff(client, new VMNetAdjHollowSyncCmd { HollowAdj = HollowLots });
            }
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
                        value = (uint)value
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

        private VMNetAvatarPersistState StateFromDB(DbAvatar avatar, List<DbRelationship> rels, List<DbJobLevel> jobs)
        {
            var state = new VMNetAvatarPersistState();
            state.Name = avatar.name;
            state.PersistID = avatar.avatar_id;
            state.DefaultSuits = new SimAntics.VMAvatarDefaultSuits(avatar.gender == DbAvatarGender.female);
            state.DefaultSuits.Daywear = avatar.body;
            state.DefaultSuits.Swimwear = avatar.body_swimwear;
            state.DefaultSuits.Sleepwear = avatar.body_sleepwear;
            state.BodyOutfit = avatar.body;
            state.HeadOutfit = avatar.head;
            state.Gender = (short)avatar.gender;
            state.Budget = (uint)avatar.budget;
            state.SkinTone = avatar.skin_tone;

            state.SkillLock = avatar.skilllock;
            state.SkillLockBody = (short)avatar.lock_body;
            state.SkillLockCharisma = (short)avatar.lock_charisma;
            state.SkillLockCooking = (short)avatar.lock_cooking;
            state.SkillLockCreativity = (short)avatar.lock_creativity;
            state.SkillLockLogic = (short)avatar.lock_logic;
            state.SkillLockMechanical = (short)avatar.lock_logic;

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
            
            if (avatar.moderation_level > 0) state.Permissions = VMTSOAvatarPermissions.Admin;
            if (avatar.lot_id == Context.DbId)
            {
                //todo: check roomie status
                state.Permissions = VMTSOAvatarPermissions.Owner;
            }
            else state.Permissions = VMTSOAvatarPermissions.Visitor;

            var motives = new short[16];
            for (int i=0; i<16; i++)
            {
                var twoi = i + i;
                motives[i] = (short)((avatar.motive_data[twoi]<<8) | avatar.motive_data[twoi + 1]);
            }
            state.MotiveData = motives;

            var relDict = new Dictionary<uint, List<uint>>();
            foreach (var rel in rels)
            {
                if (!relDict.ContainsKey(rel.to_id)) relDict[rel.to_id] = new List<uint>();
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

            state.skilllock = (byte)avatar.SkillLock;
            state.lock_body = (ushort)avatar.SkillLockBody;
            state.lock_charisma = (ushort)avatar.SkillLockCharisma;
            state.lock_cooking = (ushort)avatar.SkillLockCooking;
            state.lock_creativity = (ushort)avatar.SkillLockCreativity;
            state.lock_logic = (ushort)avatar.SkillLockLogic;
            state.lock_mechanical = (ushort)avatar.SkillLockMechanical;

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

        public void Shutdown()
        {
            //shut down this lot. Do a final save and close everything down.
            LOG.Info("Lot with dbid = " + Context.DbId + " shutting down.");
            SaveRing();
            Host.Shutdown();
        }

        //Run on the background thread
        public void AvatarLeave(IVoltronSession session)
        {
            //Exit lot, Persist the avatars data, remove avatar lock
            LOG.Info("Avatar left");
            lock (AvatarsToSave) AvatarsToSave.Add(session.AvatarId);
            VMDriver.DisconnectClient(session.AvatarId);
            Host.ReleaseAvatarClaim(session);
            ClientCount--;
        }

    }
}
