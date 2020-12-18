using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Common.Enum;
using FSO.Common.Model;
using FSO.Common.Utils;
using FSO.LotView.Model;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.LotVisitors;
using FSO.Server.Database.DA.Objects;
using FSO.Server.Database.DA.Relationships;
using FSO.Server.Database.DA.Roommates;
using FSO.Server.Database.DA.Users;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Gluon.Model;
using FSO.Server.Servers.City.Domain;
using FSO.SimAntics;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Utils;
using Microsoft.Xna.Framework;
using Ninject;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace FSO.Server.Servers.Lot.Domain
{
    /// <summary>
    /// 
    /// </summary>
    public class LotContainer
    {
        private const bool TIME_DILATION_ENABLED = true;
        private const int TIME_DILATION_THRESHOLD_MS = 500; // Accelerate through half second pauses.
        private const int TIME_DILATION_SKIP_THRESHOLD_MS = 5000; // 5 seconds, or 1 ingame minute

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
        public int KeepAliveTicker = 0;

        private bool ShuttingDown;

        private HashSet<uint> AvatarsToSave = new HashSet<uint>();
        private HashSet<IVoltronSession> SessionsToRelease = new HashSet<IVoltronSession>();
        private List<DbRelationship> RelationshipsToSave = new List<DbRelationship>();
        private DynamicTuning Tuning;

        public static readonly int TICKRATE = 30;
        public static readonly int LOT_SAVE_PERIOD = TICKRATE * 60 * 10;
        public static readonly int AVATAR_SAVE_PERIOD = TICKRATE * 60 * 1;
        public static readonly int KEEP_ALIVE_PERIOD = TICKRATE * 3;

        private IShardRealestateDomain Realestate;
        private VMTSOSurroundingTerrain Terrain;
        private bool JobLot;
        private ManualResetEvent LotActive = new ManualResetEvent(false);
        private bool ActiveYet;
        private Queue<Action> LotThreadActions = new Queue<Action>();

        private static HashSet<uint> ValidOOWGUIDs = new HashSet<uint>()
        {
            0x37EB32F3, //skill controller
            0x534564D5, //skill degrade
            0x6371EFF3, //dance floor controller
            0x3184835C, //skill tracker - type
            0x9419ADFD, //skill tracker - progress
            0x4A0C562F, //stereo speakers - music controller
            0x38E2E75B, //death - controller
            0x70F69082, //npc controller
            0x32649B09, //pest - controller
            0x55246EA3, //hat rack - handler
            0x17803AFC, //conveyor belt - controller
            0x1BD9E8F3, //conveyor belt - fx (might actually be a controller)
            0x6271EFF3, //dance floor - controller
            0x50907E06, //flies - controller
            0x3161BB5B, //job controller

            0x475CC813, //water balloon controller
            0x2D583771, //winter weather controller
            0x7A78195C, //snowball controller

            0x5157DDF2, //cat carrier
            0x3278BD34, //dog carrier

            0x699704D3, //fso vehicle controller

            0x865A6812, //car portal 1
            0xD564C66B //car portal 2
        };

        private static HashSet<uint> RequiredGUIDs = new HashSet<uint>()
        {
            0x37EB32F3, //skill controller
            0x534564D5, //skill degrade

            0x699704D3, //fso vehicle controller
            0x2D583771, //winter weather controller
        };

        private static HashSet<uint> InvalidGUIDs = new HashSet<uint>()
        {
            0xA4E8B034
        };

        private static HashSet<uint> PetCrateGUIDs = new HashSet<uint>()
        {
            0x3278BD34,
            0x5157DDF2
        };

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
                    category = LotCategory.money,
                    name = "{job:"+jobType+":"+jobLevel+"}",
                    admit_mode = 4
                };
                LotAdj = new List<DbLot>();
                LotRoommates = new List<DbRoommate>();
                Terrain = new VMTSOSurroundingTerrain();
                Tuning = new DynamicTuning(new DynTuningEntry[] {
                    new DynTuningEntry()
                    {
                        tuning_type = "feature",
                        tuning_table = 0,
                        tuning_index = 1,
                        value = 1
                    }
                });

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
                    Tuning = new DynamicTuning(db.Tuning.All());
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
        
        public string AbortVM()
        {
            Lot.Aborting = true;
            VMDriver.EndRecord();

            //if we're aborting this VM, we're getting a simantics stack trace, no matter what!

            List<VMStackFrame> ActiveStack = null;
            if (!Lot.Scheduler.RunningNow)
            {
                var cmd = VMDriver.Executing;
                if (cmd != null)
                    return "Running Command of type: " + cmd.ToString();
                return "Not running object tick or command.";
            }
            var objID = Lot.Scheduler.CurrentObjectID;
            for (int i=0; i<100; i++)
            {
                try
                {
                    var obj = Lot.GetObjectById(objID);
                    ActiveStack = new List<VMStackFrame>(obj.Thread.Stack);
                    return obj.ToString() + " Running ("+i+"): \r\n\r\n" + VMSimanticsException.GetStackTrace(ActiveStack);
                } catch (Exception)
                {
                    //try to get the trace. we might a collection enumerated exception, in which case we should try again
                }
            }

            return "Failed to obtain trace! (100 times)";
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

                        if (LotPersist.move_flags > 0)
                        {
                            //must rotate lot to face its new road direction!
                            var oldDir = ((VMTSOLotState)marshal.PlatformState).Size >> 16;
                            var newDir = VMLotTerrainRestoreTools.PickRoadDir(Terrain.Roads[1, 1]);

                            var rotate = new VMLotRotate(marshal);
                            rotate.Rotate(((newDir - oldDir) + 4) % 4);
                        }

                        Lot.Load(marshal);
                        CleanLot();
                        Lot.Reset();

                        if (File.GetCreationTimeUtc(path) < new DateTime(2018, 10, 23, 12, 00, 00))
                        {
                            ResetObjectValues();
                        } 
                    }

                    using (var db = DAFactory.Get())
                        db.Lots.UpdateRingBackup(LotPersist.lot_id, LotPersist.ring_backup_num);

                    return true;
                }
                catch (Exception e)
                {
                    LOG.Info("Ring load failed with exception: " + e.ToString() + " for lot with dbid = " + Context.DbId);
                    LotPersist.ring_backup_num--;
                    if (LotPersist.ring_backup_num < 0) LotPersist.ring_backup_num += (sbyte)Config.RingBufferSize;
                }
            }
            
            LOG.Error("FAILED to load all backups for lot with dbid = " + Context.DbId + "! Forcing lot close");
            var backupPath = Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/failedRestore" + (DateTime.Now.ToBinary().ToString()) + "/");
            Directory.CreateDirectory(backupPath);
            foreach (var file in Directory.EnumerateFiles(Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/")))
            {
                File.Copy(file, backupPath + Path.GetFileName(file));
            }

            throw new Exception("failed to load lot!");
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
                var hmarshal = Lot.HollowSave();

                Host.InBackground(() => {
                    try
                    {
                        using (var output = new FileStream(path, FileMode.Create))
                        {
                            marshal.SerializeInto(new BinaryWriter(output));
                        }

                        path = Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/hollow.fsoh");
                        using (var output = new FileStream(path, FileMode.Create))
                        {
                            hmarshal.SerializeInto(new BinaryWriter(output));
                        }

                        LotPersist.ring_backup_num = newBackup;
                        using (var db = DAFactory.Get())
                        {
                            db.Lots.UpdateRingBackup(LotPersist.lot_id, newBackup);
                            //db.Flush();
                        }
                    }
                    catch (Exception e)
                    {
                        LOG.Warn(e, "Failed to save lot (to disk/db) with dbid = " + Context.DbId);
                        LOG.Warn(e.StackTrace);
                    }
                });
                return true;
            } catch (Exception e)
            {
                LOG.Warn(e, "Failed to save lot with dbid = " + Context.DbId);
                LOG.Warn(e.StackTrace);
                return false;
            }
        }

        private void ResetObjectValues()
        {
            //the values of the objects on this lot may be invalid.

            var catalog = Content.Content.Get().WorldCatalog;
            var store = Lot.TSOState.PropertyCategory == 5;
            foreach (var obj in Lot.Entities)
            {
                if (obj is VMGameObject && obj.MultitileGroup.BaseObject == obj)
                {
                    //calculate this object's intended value
                    //statues may have set themselves to be worth a very low value (update 71)
                    //though we don't want to return value to objects with 0
                    //the best we can do is reset them to maximum sale price
                    
                    
                    var item = catalog.GetItemByGUID((obj.MasterDefinition ?? obj.Object.OBJ).GUID);
                    if (item != null)
                    {
                        var price = (int)item.Value.Price;
                        var minValue = (price * (100 - 60)) / 100;

                        if (obj.MultitileGroup.InitialPrice < minValue && obj.MultitileGroup.InitialPrice != 0)
                        {
                            obj.MultitileGroup.InitialPrice = minValue;
                        }
                    }

                }
            }
        }

        private void ReturnOOWObjects()
        {
            //we can delete these without respecting slot rules because of how SLOTs work (deleting table under us will move us to OOW)

            var ents = Lot.Entities.Where(x => x.Position == LotView.Model.LotTilePos.OUT_OF_WORLD && !ValidOOWGUIDs.Contains(x.Object.OBJ.GUID)).ToList();
            ents.AddRange(Lot.Entities.Where(x => x.MultitileGroup.Objects.Any(y => InvalidGUIDs.Contains(y.Object.OBJ.GUID))));

            foreach (var ent in ents)
            {
                ent.Delete(false, Lot.Context);
            }
        }

        private void ReturnInvalidObjects()
        {
            var objectsOnLot = new List<uint>();
            var total = 0;
            var complete = 0;

            var persists = Lot.Context.ObjectQueries.MultitileByPersist.Keys.ToList();
            Dictionary<uint, DbObject> ownerInfo;
            using (var da = DAFactory.Get())
            {
                ownerInfo = da.Objects.GetObjectOwners(persists).ToDictionary(x => x.object_id);
            }

            var ents = new List<VMEntity>(Lot.Entities);
            var needToCreate = new HashSet<uint>(RequiredGUIDs);
            var removeAll = (LotPersist.move_flags & 6) > 0;
            foreach (var ent in ents)
            {
                needToCreate.Remove(ent.Object.OBJ.GUID);
                if (ent.PersistID >= 16777216 && ent is VMGameObject)
                {
                    if (LotPersist.admit_mode == 5) {
                        Lot.Context.ObjectQueries.RemoveMultitilePersist(Lot, ent.PersistID);
                        ent.PersistID = 0;
                        ((VMTSOObjectState)ent.TSOState).OwnerID = 0;
                        ((VMGameObject)ent).Disabled = 0;
                        continue;
                    }
                    if (ent.MultitileGroup.Objects.Count == 0 || ent != ent.MultitileGroup.BaseObject) continue;

                    //look for this object in the owner info
                    var deleteMode = 0;
                    DbObject info = null;
                    if (!ownerInfo.TryGetValue(ent.PersistID, out info))
                    {
                        deleteMode = 1;
                    } else
                    {
                        if (((VMTSOObjectState)ent.TSOState).OwnerID != info.owner_id)
                        {
                            foreach (var e in ent.MultitileGroup.Objects) ((VMTSOObjectState)e.TSOState).OwnerID = info.owner_id ?? 0;
                        }

                        //send back if they arent meant to be here
                        //or if the object is not donated and the owner is not a roomie
                        if (info.lot_id != Context.DbId)
                            deleteMode = 2;
                        else if (removeAll || !(Lot.TSOState.Roommates.Contains(((VMTSOObjectState)ent.TSOState).OwnerID) 
                            || ((VMTSOObjectState)ent.TSOState).ObjectFlags.HasFlag(VMTSOObjectFlags.FSODonated)))
                            deleteMode = 1;
                    }

                    objectsOnLot.Add(ent.PersistID);
                    if (deleteMode > 0)
                    {
                        //we need to send objects in slots back to their owners inventory too, so we don't lose what was on tables etc.
                        var sendback = new List<VMEntity>();
                        sendback.Add(ent);
                        ObjListAllContained(sendback, ent, 0);

                        foreach (var delE in sendback)
                        {
                            if (delE.MultitileGroup.Objects.Count == 0) continue;
                            if (delE.PersistID >= 16777216 && delE is VMGameObject)
                            {
                                total++;
                                //this is run synchro.
                                if (deleteMode == 1)
                                {
                                    //return to inventory, since the object is actually on this lot
                                    VMGlobalLink.MoveToInventory(Lot, delE.MultitileGroup, (success, objid) =>
                                    {
                                        Lot.Context.ObjectQueries.RemoveMultitilePersist(Lot, delE.PersistID);
                                        foreach (var o in delE.MultitileGroup.Objects) o.PersistID = 0; //no longer representative of the object in db.
                                        delE.Delete(true, Lot.Context);
                                        complete++;
                                    }, true);
                                } else
                                {

                                    //object is already elsewhere... do not save its state.
                                    Lot.Context.ObjectQueries.RemoveMultitilePersist(Lot, delE.PersistID);
                                    foreach (var obj in delE.MultitileGroup.Objects)
                                        obj.PersistID = 0;
                                    delE.Delete(true, Lot.Context);
                                }
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

            foreach (var obj in needToCreate) {
                Lot.Context.CreateObjectInstance(obj, LotTilePos.OUT_OF_WORLD, Direction.NORTH);
            }

            if ((LotPersist.move_flags & 2) > 0)
            {
                BlueprintReset();
                LotPersist.move_flags = 0;
            }
        }

        private void ObjListAllContained(List<VMEntity> ents, VMEntity ent, int depth)
        {
            if (depth > 50) throw new Exception("slot depth too high!");
            for (int i=0; i<ent.TotalSlots(); i++)
            {
                var slotE = ent.GetSlot(i);
                if (slotE != null)
                {
                    ents.Add(slotE);
                    ObjListAllContained(ents, slotE, depth++); //recursive
                }
            }
        }

        private void CleanLot()
        {
            LOG.Info("Cleaning lot with dbid = " + Context.DbId);
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

        public void BlueprintReset()
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
                jobLevel = (short)((jobPacked - 1) & 0xF);
                var jobType = (short)((jobPacked - 1) / 0xF);
                var randomChance = (jobType > 2 && jobLevel > 6) ? 2 : 1;
                var lotID = JobMatchmaker.JobGradeToLotGroup[jobType][jobLevel] + (new Random()).Next(randomChance);

                path = Content.Content.Get().GetPath("housedata/blueprints/" + JobMatchmaker.JobXMLName[jobType]
                    + lotID.ToString().PadLeft(2, '0') + "_"
                    + "00"
                    + ".xml");
            }
            Lot.SendCommand(new VMBlueprintRestoreCmd
            {
                JobLevel = jobLevel,
                XMLData = File.ReadAllBytes(path),

                FloorClipX = floorClip.X,
                FloorClipY = floorClip.Y,
                FloorClipWidth = floorClip.Width,
                FloorClipHeight = floorClip.Height,
                OffsetX = offset.X,
                OffsetY = offset.Y,
                TargetSize = targetSize
            });
            Lot.Tick();
            SaveRing();
        }


        public void ResetVM()
        {
            LOG.Info("Resetting VM for lot with dbid = " + Context.DbId);
            VMGlobalLink = Kernel.Get<LotServerGlobalLink>();
            VMDriver = new VMServerDriver(VMGlobalLink);
            VMDriver.OnTickBroadcast += TickBroadcast;
            VMDriver.OnDirectMessage += DirectMessage;
            VMDriver.OnDropClient += DropClient;

            if (JobLot && Config.LogJobLots) {
                var jobPacked = Context.DbId - 0x200;
                var jobLevel = (short)((jobPacked - 1) & 0xF);
                var jobType = (short)((jobPacked - 1) / 0xF);
                var timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH-mm");

                Directory.CreateDirectory(Path.Combine(Config.SimNFS, "LotLogs/"));
                var filename = Path.Combine(Config.SimNFS, "LotLogs/[" + timestamp + "] Job " + jobType + "_" + jobLevel + " id" + Context.DbId + ".fsor");
                LOG.Info("Recording Job Lot at " + filename);
                var stream = new FileStream(filename, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
                VMDriver.Record(stream);
            }

            Lot = new VM(new VMContext(null), VMDriver, new VMNullHeadlineProvider());
            Lot.OnChatEvent += Lot_OnChatEvent;
            Lot.Init();

            bool isNew = false;
            bool isMoved = (LotPersist.move_flags > 0);
            LoadAdj();
            if (!JobLot && LotPersist.ring_backup_num > -1 && AttemptLoadRing())
            {
                LOG.Info("Successfully loaded and cleaned fsov for dbid = " + Context.DbId);
            }
            else
            {
                isNew = true;
                BlueprintReset();
            }

            Lot.TSOState.Terrain = Terrain;
            Lot.TSOState.Name = LotPersist.name;
            Lot.TSOState.NhoodID = LotPersist.neighborhood_id;
            Lot.TSOState.LotID = LotPersist.location;
            Lot.TSOState.SkillMode = LotPersist.skill_mode;
            Lot.TSOState.PropertyCategory = (byte)LotPersist.category;
            var isCommunity = LotPersist.category == LotCategory.community;

            if (isCommunity)
            {
                var owner = LotPersist.owner_id ?? 0;
                if (Lot.TSOState.OwnerID != owner)
                {
                    //a new mayor owns this property.
                    //clear the donators lists (roomies lists)

                    Lot.TSOState.Roommates.Clear();
                    Lot.TSOState.BuildRoommates.Clear();

                    Lot.TSOState.Roommates.Add(owner);
                    Lot.TSOState.BuildRoommates.Add(owner);
                    Lot.TSOState.OwnerID = owner;
                }

                EnsureCommunityObjects();
            }
            else
            {
                Lot.TSOState.OwnerID = LotPersist.owner_id ?? 0;
                Lot.TSOState.Roommates = new HashSet<uint>();
                Lot.TSOState.BuildRoommates = new HashSet<uint>();
                foreach (var roomie in LotRoommates)
                {
                    if (roomie.is_pending > 0) continue;
                    Lot.TSOState.Roommates.Add(roomie.avatar_id);
                    if (roomie.permissions_level > 0)
                        Lot.TSOState.BuildRoommates.Add(roomie.avatar_id);
                    if (roomie.permissions_level > 1)
                        Lot.TSOState.OwnerID = roomie.avatar_id;
                }
            }

            Lot.TSOState.ActivateValidator(Lot);

            Lot.Context.UpdateTSOBuildableArea();

            Lot.MyUID = uint.MaxValue - 1;
            if ((LotPersist.move_flags & 2) > 0) isNew = true;
            ReturnInvalidObjects();
            if (!JobLot) ReturnOOWObjects();

            var restoreType = isCommunity ? RestoreLotType.Community : RestoreLotType.Normal;
            if (isMoved || isNew) VMLotTerrainRestoreTools.RestoreTerrain(Lot, restoreType);
            VMLotTerrainRestoreTools.EnsureCoreObjects(Lot, restoreType);
            if (isNew) VMLotTerrainRestoreTools.PopulateBlankTerrain(Lot);

            ResyncTime();

            if (Lot.Tuning == null || (Lot.Tuning.GetTuning("forcedTuning", 0, 0) ?? 0f) == 0f)
            {
                Lot.ForwardCommand(new VMNetTuningCmd()
                {
                    Tuning = Tuning
                });
            }

            Lot.Context.UpdateTSOBuildableArea();

            var entClone = new List<VMEntity>(Lot.Entities);
            foreach (var ent in entClone)
            {
                if (ent is VMGameObject)
                {
                    ((VMGameObject)ent).Disabled &= ~VMGameObjectDisableFlags.TransactionIncomplete;
                    ((VMGameObject)ent).DisableIfTSOCategoryWrong(Lot.Context);
                    if (ent.Object.OBJ.GUID == 0x34D777C3 && ent.GetValue(VMStackObjectVariable.Hidden) > 0)
                        ent.SetValue(VMStackObjectVariable.Hidden, 0);
                    if (PetCrateGUIDs.Contains(ent.Object.OBJ.GUID) && ent.GetAttribute(1) == 0)
                    {
                        //if this pet isn't out, but their crate is out of world, place it near the mailbox.
                        if (ent.Position == LotTilePos.OUT_OF_WORLD)
                        {
                            //put it close to the mailbox
                            var mailbox = Lot.Entities.FirstOrDefault(x => (x.Object.OBJ.GUID == 0xEF121974 || x.Object.OBJ.GUID == 0x1D95C9B0));
                            if (mailbox != null) SimAntics.Primitives.VMFindLocationFor.FindLocationFor(ent, mailbox, Lot.Context, VMPlaceRequestFlags.UserPlacement);
                        }
                    }
                    if (ent.GetFlag(VMEntityFlags.Occupied))
                    {
                        if (PetCrateGUIDs.Contains(ent.Object.OBJ.GUID))
                        {
                            //typically pet crates or other things which should never have state deleted.
                            ent.SetFlag(VMEntityFlags.Occupied, false);
                            if (ent.Position == LotTilePos.OUT_OF_WORLD)
                            {
                                //put it close to the mailbox
                                var mailbox = Lot.Entities.FirstOrDefault(x => (x.Object.OBJ.GUID == 0xEF121974 || x.Object.OBJ.GUID == 0x1D95C9B0));
                                if (mailbox != null) SimAntics.Primitives.VMFindLocationFor.FindLocationFor(ent, mailbox, Lot.Context, VMPlaceRequestFlags.UserPlacement);
                            }
                            //ent.ExecuteEntryPoint(2, Lot.Context, true);
                        }
                        else
                        {
                            if (ent.Object.OBJ.GUID != 0x30A76C84 && ent.Object.OBJ.GUID != 0x130B5C88) //ignore these two for now
                            {
                                ent.ResetData();
                                ent.Init(Lot.Context); //objects should not be occupied when we join the lot...
                            }
                        }
                    }
                    {
                        ent.ExecuteEntryPoint(2, Lot.Context, true);
                    }
                }
            }
            LotActive.Set();
            ActiveYet = true;

            if (JobLot)
            {
                //for recording. must resave lot to get appropriate state changes from terrain population 
                //(important for playback to sync)
                Lot.Tick();
                Lot.ForwardCommand(new VMStateSyncCmd()
                {
                    State = Lot.Save(),
                    Run = false,
                });
            }
        }

        public void UpdateTuning(IEnumerable<DynTuningEntry> tuning)
        {
            Tuning = new DynamicTuning(tuning);
            if (Lot == null || JobLot) return;
            if (Lot.Tuning == null || (Lot.Tuning.GetTuning("forcedTuning", 0, 0) ?? 0f) == 0f)
            {
                Lot.ForwardCommand(new VMNetTuningCmd()
                {
                    Tuning = Tuning
                });
            }
        }

        private void ResyncTime()
        {
            var time = DateTime.UtcNow;
            var tsoTime = TSOTime.FromUTC(time);

            Lot.ForwardCommand(new VMNetSetTimeCmd()
            {
                Hours = tsoTime.Item1,
                Minutes = tsoTime.Item2,
                Seconds = tsoTime.Item3,
                UTCStart = DateTime.UtcNow.Ticks
            });
        }

        private static uint PAYPHONE_GUID = 0x313D2F9A;
        private static uint NHOOD_PAYPHONE_GUID = 0x303CD603;
        private static uint NHOOD_BULLETIN_GUID = 0x4B489F30;
        private static uint NHOOD_BULLETIN_SMART_GUID = 0x792617D7;

        private void EnsureCommunityObjects()
        {
            var payphones = Lot.Context.ObjectQueries.GetObjectsByGUID(PAYPHONE_GUID)?.ToList(); //clone as we will be removing them
            if (payphones != null)
            {
                foreach (var phone in payphones)
                {
                    var pos = phone.Position;
                    var dir = phone.Direction;
                    phone.Delete(true, Lot.Context);

                    Lot.Context.CreateObjectInstance(NHOOD_PAYPHONE_GUID, pos, dir);
                }
            }

            var bulletin = Lot.Context.ObjectQueries.GetObjectsByGUID(NHOOD_BULLETIN_SMART_GUID)?.FirstOrDefault();
            if (bulletin == null)
            {
                var mailbox = Lot.Entities.FirstOrDefault(x => (x.Object.OBJ.GUID == 0xEF121974 || x.Object.OBJ.GUID == 0x1D95C9B0));
                bulletin = Lot.Context.CreateObjectInstance(NHOOD_BULLETIN_GUID, LotTilePos.OUT_OF_WORLD, Direction.NORTH).BaseObject;
                SimAntics.Primitives.VMFindLocationFor.FindLocationFor(bulletin, mailbox, Lot.Context, VMPlaceRequestFlags.UserPlacement);
            }

        }

        private void Lot_OnChatEvent(VMChatEvent evt)
        {
            if (evt.Type == VMChatEventType.Debug)
            {
                LOG.Info("LOT " + Context.DbId + ": " + evt.Text[0]);
            }
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

        private void DereferenceLot()
        {
            //mono somehow keeps a reference to the LotContainer... (probably)
            //this causes a substantial memory leak on linux
            //since i can't profile this for some reason, dereference the lot
            //data so we can minimize the impact.

            Lot = null;
            LotPersist = null;
            LotAdj = null;
            LotRoommates = null;

            VMDriver = null;
            VMGlobalLink = null;
            HollowLots = null;
        }

        /// <summary>
        /// Load and initialize everything to start up the lot
        /// </summary>
        public void Run()
        {
            try
            {
                try
                {
                    ResetVM();
                }
                catch (Exception e)
                {
                    LOG.Info("LOT " + Context.DbId + " LOAD EXECPTION:" + e.ToString());
                    Host.Shutdown();
                    DereferenceLot();
                    return;
                }
                LOG.Info("Starting to host lot with dbid = " + Context.DbId);
                Host.SetOnline(true);

                var timeKeeper = new Stopwatch(); //todo: smarter timing
                timeKeeper.Start();
                long lastTick = 0;
                long skippedTimeMs = 0;

                LotSaveTicker = LOT_SAVE_PERIOD;
                AvatarSaveTicker = AVATAR_SAVE_PERIOD;
                while (true)
                {
                    bool noRemainingUsers = ClientCount == 0;
                    lastTick++;
                    //sometimes avatars can be killed immediately after their kill timer starts (this frame will run the leave lot interaction)
                    //this works around that possibility. 
                    var preTickAvatars = Lot.Context.ObjectQueries.AvatarsByPersist.Values.Select(x => x).ToList();
                    var noRoomies = !(preTickAvatars.Any(x => ((VMTSOAvatarState)x.TSOState).Permissions > VMTSOAvatarPermissions.Visitor)) 
                        && (LotPersist.admit_mode < 4 && LotPersist.category != LotCategory.community);

                    try
                    {
                        Lot.Tick();
                    }
                    catch (Exception e)
                    {
                        //something bad happened. not entirely sure how we should deal with this yet
                        LOG.Error("VM ERROR: " + e.Message +  e.StackTrace);
                        Host.Shutdown();
                        DereferenceLot();
                        return;
                    }

                    if (Lot.Aborting)
                    {
                        DereferenceLot();
                        return; //background thread has already released all our avatars and our claim. exit immediately.
                    }

                    if (noRoomies && !noRemainingUsers)
                    {
                        if (TimeToShutdown == -1)
                        {
                            TimeToShutdown = (Context.Action == ClaimAction.LOT_CLEANUP) ? 1 : TICKRATE * 40;
                        }

                        if (--TimeToShutdown < TICKRATE * 10)
                        {
                            //no roommates are here, so all visitors must be kicked out.
                            if (preTickAvatars.Count > 0)
                            {
                                Host.Broadcast(new HashSet<uint>(), new FSOVMProtocolMessage(true, "21", "22"));
                            }
                            foreach (var avatar in preTickAvatars)
                            {
                                if (avatar.KillTimeout == -1) avatar.UserLeaveLot();
                                VMDriver.DropAvatar(avatar);
                            }
                        }
                    }

                    if (noRemainingUsers)
                    {
                        if (TimeToShutdown == -1)
                        {
                            //lot shuts down 20 seconds after everyone leaves
                            //if we're doing a cleanup action, it closes immediately
                            TimeToShutdown = (Context.Action == ClaimAction.LOT_CLEANUP) ? 1 : TICKRATE * 20;
                        }
                        else
                        {
                            if (--TimeToShutdown == 0 || (ShuttingDown && TimeToShutdown < (TICKRATE * 20 - 10)))
                            {
                                Shutdown();
                                DereferenceLot();
                                return; //kill the lot
                            }
                        }
                    }
                    else if (!noRoomies && TimeToShutdown != -1)
                        TimeToShutdown = -1;

                    if (--LotSaveTicker <= 0)
                    {
                        SaveRing();
                        LotSaveTicker = LOT_SAVE_PERIOD;

                        Host.UpdateActiveVisitRecords();
                    }

                    var beingKilled = preTickAvatars.Where(x => x.KillTimeout == 1);
                    if (beingKilled.Count() > 0)
                    {
                        //avatars that are being killed could die before their user disconnects. It's important to save them immediately.
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

                    List<IVoltronSession> toRelease = null;
                    lock (SessionsToRelease)
                    {
                        //save avatar state, then release their avatar claims afterwards.
                        //SaveAvatars(SessionsToRelease.Select(x => Lot.GetAvatarByPersist(x.AvatarId)), true); //todo: is this performed by the fact that we started the persist save above?
                        if (SessionsToRelease.Count > 0)
                        {
                            toRelease = new List<IVoltronSession>(SessionsToRelease);
                            SessionsToRelease.Clear();
                        }
                    }

                    if (toRelease != null) {
                        foreach (var session in toRelease)
                        {
                            Host.ReleaseAvatarClaim(session);
                        }
                    }

                    Queue<Action> lotActions = null;
                    lock (LotThreadActions) {
                        if (LotThreadActions.Count > 0)
                        {
                            lotActions = new Queue<Action>(LotThreadActions);
                            LotThreadActions.Clear();
                        }
                    }

                    if (lotActions != null) {
                        while (lotActions.Count > 0) lotActions.Dequeue()();
                    }

                    if (--KeepAliveTicker <= 0)
                    {
                        Host.InBackground(null);
                        KeepAliveTicker = KEEP_ALIVE_PERIOD;
                    }

                    long currentTickMs = ((lastTick + 1) * 1000) / TICKRATE;
                    long targetTickMs = timeKeeper.ElapsedMilliseconds;

                    long sleepTime = currentTickMs - targetTickMs;

                    if (sleepTime > 0)
                    {
                        Thread.Sleep((int)Math.Max(0, sleepTime));
                    }
                    else
                    {
                        if (-sleepTime > TIME_DILATION_THRESHOLD_MS && TIME_DILATION_ENABLED)
                        {
                            // skip forward in time
                            long skipTime = ((-sleepTime) * TICKRATE) / 1000;
                            lastTick += skipTime;
                            skippedTimeMs -= sleepTime;

                            if (skippedTimeMs > TIME_DILATION_SKIP_THRESHOLD_MS)
                            {
                                ResyncTime();
                                skippedTimeMs = 0;
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                LOG.Info("Fatal exception on lot " + Context.DbId + ":" + e.ToString());
                Host.Shutdown();
                DereferenceLot();
                return;
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
            if (!ActiveYet) return false; //we are not on an inactive lot.
            BlockOnLotThread(() =>
            {
                result = Lot.Context.ObjectQueries.AvatarsByPersist.ContainsKey(pid);
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
                var inventory = da.Objects.GetAvatarInventoryWithAttrs(session.AvatarId);
                var myRoomieLots = da.Roommates.GetAvatarsLots(session.AvatarId); //might want to use other entries to update the roomies table entirely.
                var myIgnored = da.Bookmarks.GetAvatarIgnore(session.AvatarId);
                var user = da.Users.GetById(avatar.user_id);
                LOG.Info("Avatar " + avatar.name + " ("+session.AvatarId+") has joined lot "+Context.DbId);

                //Load all the avatars data
                var state = StateFromDB(avatar, user, rels, jobinfo, myRoomieLots, myIgnored);

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

                var visitorType = DbLotVisitorType.visitor;
                if (myRoomieLots.Count > 0)
                {
                    var roomieStatus = myRoomieLots.FindAll(x => x.lot_id == Context.DbId).FirstOrDefault();
                    if (roomieStatus != null && roomieStatus.is_pending == 0)
                    {
                        switch (roomieStatus.permissions_level)
                        {
                            case 0:
                            case 1:
                                visitorType = DbLotVisitorType.roommate;
                                break;
                            case 2:
                                visitorType = DbLotVisitorType.owner;
                                break;
                        }
                    }
                }
                Host.RecordStartVisit(session, visitorType);

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
                Graphic = obj.graphic,

                AttributeMode = obj.has_db_attributes,
                Attributes = obj.AugmentedAttributes ?? new List<int>()
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

        private VMNetAvatarPersistState StateFromDB(DbAvatar avatar, User user, List<DbRelationship> rels, List<DbJobLevel> jobs, List<DbRoommate> myRoomieLots, List<uint> ignored)
        {
            var state = new VMNetAvatarPersistState();
            state.Name = avatar.name;
            state.PersistID = avatar.avatar_id;
            state.DefaultSuits = new SimAntics.VMAvatarDefaultSuits(avatar.gender == DbAvatarGender.female);
            state.DefaultSuits.Daywear.ID = avatar.body;
            state.DefaultSuits.Swimwear.ID = avatar.body_swimwear;
            state.DefaultSuits.Sleepwear.ID = avatar.body_sleepwear;
            state.BodyOutfit = (avatar.body_current == 0)?avatar.body:avatar.body_current;
            state.HeadOutfit = avatar.head;
            state.Gender = (short)avatar.gender;
            state.Budget = (uint)avatar.budget;
            state.SkinTone = avatar.skin_tone;
            state.CustomGUID = avatar.custom_guid ?? 0;

            var now = Epoch.Now;
            var rage = (uint)((now - user.register_date) / ((long)60 * 60 * 24));
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

            if (avatar.mayor_nhood == LotPersist.neighborhood_id)
                state.AvatarFlags |= VMTSOAvatarFlags.Mayor; //we're not roommate anywhere, so we can be here.

            if (myRoomieLots.Count == 0 && LotPersist.category != LotCategory.community)
                state.AvatarFlags |= VMTSOAvatarFlags.CanBeRoommate; //we're not roommate anywhere, so we can be here.

            if (rage < 7)
                state.AvatarFlags |= VMTSOAvatarFlags.NewPlayer;

            if (LotPersist.category == LotCategory.community)
            {
                if (LotPersist.owner_id == avatar.avatar_id)
                {
                    state.Permissions = VMTSOAvatarPermissions.Owner;
                } else state.Permissions = VMTSOAvatarPermissions.Visitor; //needs to be set by the VM.
            }
            else
            {
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
            }

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
            state.body = avatar.DefaultSuits.Daywear.ID;
            state.body_sleepwear = avatar.DefaultSuits.Sleepwear.ID;
            state.body_swimwear = avatar.DefaultSuits.Swimwear.ID;
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

        public void NotifyRoommateChange(uint avatar_id, uint replace_id, ChangeType change)
        {
            var signalled = LotActive.WaitOne(10000); //wait til we're active at least
            if (!signalled) return; //give up
            VMTSOAvatarPermissions newLevel = VMTSOAvatarPermissions.Visitor;
            VMChangePermissionsMode mode = VMChangePermissionsMode.NORMAL;
            switch (change)
            {
                case ChangeType.ADD_ROOMMATE:
                    newLevel = VMTSOAvatarPermissions.Roommate; break;
                case ChangeType.REMOVE_ROOMMATE:
                    newLevel = VMTSOAvatarPermissions.Visitor; break;
                case ChangeType.BECOME_OWNER:
                    newLevel = VMTSOAvatarPermissions.Owner;
                    mode = VMChangePermissionsMode.OWNER_SWITCH; break;
                case ChangeType.BECOME_OWNER_WITH_OBJECTS:
                    newLevel = VMTSOAvatarPermissions.Owner;
                    mode = VMChangePermissionsMode.OWNER_SWITCH_WITH_OBJECTS; break;
                case ChangeType.ROOMIE_INHERIT_OBJECTS_ONLY:
                    mode = VMChangePermissionsMode.OBJECTS_ONLY; break;
            }

            try
            {
                VMDriver.SendCommand(new VMChangePermissionsCmd
                {
                    TargetUID = avatar_id,
                    Level = newLevel,
                    Mode = mode,
                    ReplaceUID = replace_id,
                    Verified = true,
                });
            } catch (Exception)
            {

            }
        }

        public void ForceShutdown()
        {
            //this lot needs to be shutdown asap. As soon as all avatars are disconnected/saved, clean lot and shutdown.
            ShuttingDown = true;
        }

        public void Shutdown()
        {
            //shut down this lot. Do a final save and close everything down.
            VMDriver.EndRecord();
            LOG.Info("Lot with dbid = " + Context.DbId + " shutting down.");
            if ((LotPersist.move_flags & 4) > 0)
            {
                //this lot is slated to be deleted from the database.
                using (var da = DAFactory.Get())
                {
                    da.Lots.Delete(Context.DbId);
                    var lotStr = LotPersist.lot_id.ToString("x8");
                    Directory.Delete(Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/"), true);
                }
            }
            try
            {
                ReturnInvalidObjects();
            }
            catch (Exception e) { }
            SaveRing();

            //if we have a null owner, this lot needs to be deleted.

            if (!(JobLot || LotPersist.category == LotCategory.community)) {
                using (var da = DAFactory.Get())
                {
                    var lot = da.Lots.Get(Context.DbId);
                    if (lot.owner_id == null)
                    {

                        try
                        {
                            var lotStr = LotPersist.lot_id.ToString("x8");
                            Directory.Delete(Path.Combine(Config.SimNFS, "Lots/" + lotStr + "/"), true);
                        } catch (Exception)
                        {
                            
                        }
                        //note that the lot has to be deleted from db by lot allocations, since it still needs to unlock the location this property was at.
                    }
                }
            }

            Host.Shutdown();
        }

        //Run on the background thread
        public void AvatarLeave(IVoltronSession session)
        {
            //Exit lot, Persist the avatars data, remove avatar lock
            LOG.Info("Avatar "+session.AvatarId+" left lot "+Context.DbId);

            // defer the following so that the avatar save is queued, then their session's claim is released.
            lock (SessionsToRelease) SessionsToRelease.Add(session);

            VMDriver.DisconnectClient(session.AvatarId);
            ClientCount--;
        }

        public void AvatarRefresh(IVoltronSession session)
        {
            //Exit lot, Persist the avatars data, remove avatar lock
            LOG.Info("Avatar " + session.AvatarId + " re-established connection to lot " + Context.DbId);

            VMDriver.RefreshClient(session.AvatarId);
        }
    }
}
