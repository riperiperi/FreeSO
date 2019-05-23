#define VM_DESYNC_DEBUG

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Files.Formats.IFF.Chunks;
using FSO.SimAntics.Engine;
/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using Microsoft.Xna.Framework;
using FSO.Content;
using FSO.Vitaboy;
using FSO.SimAntics.Model;
using FSO.SimAntics.NetPlay;
using FSO.SimAntics.NetPlay.Model;
using System.Collections.Concurrent;
using FSO.SimAntics.Marshals;
using FSO.LotView.Components;
using FSO.SimAntics.Marshals.Threads;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Engine.TSOTransaction;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.Model.Sound;
using FSO.SimAntics.NetPlay.EODs;
using FSO.SimAntics.NetPlay.Drivers;
using FSO.SimAntics.NetPlay.Model.Commands;
using FSO.SimAntics.Marshals.Hollow;
using FSO.SimAntics.Engine.Debug;
using FSO.Common;
using FSO.SimAntics.Primitives;
using FSO.LotView.Model;
using FSO.HIT;
using FSO.Common.Model;
using FSO.SimAntics.Model.TS1Platform;
using FSO.SimAntics.Model.Platform;
using FSO.Common.Utils;

namespace FSO.SimAntics
{
    /// <summary>
    /// Simantics Virtual Machine.
    /// </summary>
    public class VM
    {
        public bool UseSchedule = true;
        private static bool _UseWorld = true;
        public static bool SignalBreaks = false;
        public static bool UseWorld
        {
            get { return _UseWorld; }
            set
            {
                _UseWorld = value;
                VMContext.UseWorld = value;
                VMEntity.UseWorld = value;
            }
        }

        public static string TestBinding;

        public bool IsServer
        {
            get { return GlobalLink != null; }
        }
        public bool BlueprintRestore
        {
            get { return GlobalLink != null || Driver is VMFSORDriver; }
        }
        public bool TS1;
        public static bool GlobTS1; //I don't like this, but we don't pass VM to some things and this needs to be fast
        //we can assume one application won't be running TS1 and TSO at the same time.
        public bool Aborting = false;

        private const long TickInterval = 33 * TimeSpan.TicksPerMillisecond;
        public byte[][] HollowAdj;

        public VMContext Context { get; internal set; }

        public List<VMEntity> Entities = new List<VMEntity>();
        public HashSet<VMEntity> SoundEntities = new HashSet<VMEntity>();
        public short[] GlobalState;
        public VMAbstractLotState PlatformState;

        public VMScheduler Scheduler;

        public VMTSOLotState TSOState
        {
            get { return PlatformState as VMTSOLotState; }
        }

        public VMTS1LotState TS1State
        {
            get { return PlatformState as VMTS1LotState; }
        }

        public string LotName
        {
            get
            {
                return TSOState?.Name ?? "";
            }
        }

        public DynamicTuning Tuning;
        public VMTuningCache TuningCache = new VMTuningCache();
        private Dictionary<short, VMEntity> ObjectsById = new Dictionary<short, VMEntity>();
        private short ObjectId = 1;

        internal VMNetDriver Driver;
        public VMHeadlineRendererProvider Headline;

        public bool Ready;
        public bool BHAVDirty;

        //attributes for the current VM session.
        public uint MyUID; //UID of this client in the VM
        public VMSyncTrace Trace;
        public List<VMLoadError> LoadErrors = new List<VMLoadError>();
        public List<VMInventoryItem> MyInventory = new List<VMInventoryItem>();

        public event VMDialogHandler OnDialog;
        public event VMChatEventHandler OnChatEvent;
        public event VMRefreshHandler OnFullRefresh;
        public event VMBreakpointHandler OnBreakpoint;
        public event VMEODMessageHandler OnEODMessage;
        public event VMLotSwitchHandler OnRequestLotSwitch;
        public event VMGenericEvtHandler OnGenericVMEvent;
        
        public delegate void VMDialogHandler(VMDialogInfo info);
        public delegate void VMChatEventHandler(VMChatEvent evt);
        public delegate void VMRefreshHandler();
        public delegate void VMBreakpointHandler(VMEntity entity);
        public delegate void VMEODMessageHandler(VMNetEODMessageCmd msg);
        public delegate void VMLotSwitchHandler(uint lotId);
        public delegate void VMGenericEvtHandler(VMEventType type, object data);

        public IVMTSOGlobalLink GlobalLink
        {
            get
            {
                return Driver.GlobalLink;
            }
        }
        public VMEODHost EODHost; //only present if we're a server
        public VMTSOGlobalLinkStub CheckGlobalLink = new VMTSOGlobalLinkStub();

        /// <summary>
        /// Constructs a new Virtual Machine instance.
        /// </summary>
        /// <param name="context">The VMContext instance to use.</param>
        public VM(VMContext context, VMNetDriver driver, VMHeadlineRendererProvider headline)
        {
            context.VM = this;
            Context = context;
            Driver = driver;
            Headline = headline;
            Scheduler = new VMScheduler(this);
            GameTickRate = FSOEnvironment.RefreshRate;

            TS1 = Content.Content.Get().TS1;
            GlobTS1 = TS1;
        }

        private void VM_OnBHAVChange()
        {
            BHAVDirty = true;
        }

        /// <summary>
        /// Gets an entity from this VM.
        /// </summary>
        /// <param name="id">The entity's ID.</param>
        /// <returns>A VMEntity instance associated with the ID.</returns>
        public VMEntity GetObjectById(short id)
        {
            VMEntity result;
            if (ObjectsById.TryGetValue(id, out result))
            {
                return result;
            }
            return null;
        }

        public VMEntity GetObjectByPersist(uint id)
        {
            return Entities.FirstOrDefault(x => x.PersistID == id);
        }

        public VMAvatar GetAvatarByPersist(uint id)
        {
            VMAvatar result = null;
            Context.ObjectQueries.AvatarsByPersist.TryGetValue(id, out result);
            return result;
        }

        /// <summary>
        /// Initializes this Virtual Machine.
        /// </summary>
        public void Init()
        {
            PlatformState = (TS1)?(VMAbstractLotState)new VMTS1LotState():new VMTSOLotState();
            GlobalState = new short[38];
            GlobalState[20] = 255; //Game Edition. Basically, what "expansion packs" are running. Let's just say all of them.
            GlobalState[25] = 4; //as seen in EA-Land edith's simulator globals, this needs to be set for people to do their idle interactions.
            GlobalState[17] = 4; //Runtime Code Version, is this in EA-Land.
            if (Driver is VMServerDriver) EODHost = new VMEODHost();
            PlatformState.ActivateValidator(this);
        }

        public void Reset()
        {
            //some objects expect that the server delete all avatars upon loading the lot (pets, food counters)
            //var avatars = new List<VMEntity>(Entities.Where(x => x is VMAvatar && x.PersistID > 0));
            var avatars = new List<VMEntity>(Context.ObjectQueries.Avatars);
            foreach (var avatar in avatars) avatar.Delete(true, Context);

            var ents = new List<VMEntity>(Entities);
            foreach (var ent in ents)
            {
                if (ent.Thread.BlockingState != null) ent.Thread.BlockingState = null;
                if (ent.Thread.EODConnection != null) ent.Thread.EODConnection = null;
                if (ent.Object.OBJ.GUID == 0x3929AADC) ent.Delete(true, Context); //also remove any reserved tiles
            }
        }

        private int GameTickRate = 60;
        private int GameTickNum = 0;
        public int SpeedMultiplier = 1;
        public int LastSpeedMultiplier;
        private int LastFrameSpeed = 1;
        private float Fraction;
        public VMEntity GlobalBlockingDialog;
        public void Update()
        {
            if (UseWorld)
            {
                Microsoft.Xna.Framework.Audio.SoundEffect.DistanceScale = 10;
                var listener = HIT.HITVM.Get().Listener;
                var cam = Context.World.State.Camera;
                listener.Position = cam.Position;
                var forward = cam.Target - cam.Position;
                forward.X *= -1f;
                forward.Normalize();
                listener.Forward = forward;
                Context.World.State.SimSpeed = SpeedMultiplier;
            }

            if (LastFrameSpeed != SpeedMultiplier)
            {
                var allSounds = new List<HITSound>();
                foreach (var ent in SoundEntities)
                {
                    allSounds.AddRange(ent.SoundThreads.Select(x => x.Sound));
                }

                if (SpeedMultiplier < 1 && LastFrameSpeed >= 1) allSounds.ForEach((x) => x.Pause()); 
                else if (SpeedMultiplier >= 1 && LastFrameSpeed < 1) allSounds.ForEach((x) => x.Resume());
                LastFrameSpeed = SpeedMultiplier;
            }

            var mul = Math.Max(SpeedMultiplier, 1);
            var oldFrame = (GameTickNum * 30 * mul) / GameTickRate;
            GameTickNum++;
            var newFrame = (GameTickNum * 30 * mul) / GameTickRate;
            for (int i = 0; i < newFrame - oldFrame; i++)
            {
                Tick();
                if (SpeedMultiplier <= 0) break;
            }

            Fraction = ((GameTickNum * 30 * SpeedMultiplier) - (newFrame * GameTickRate)) / (float)GameTickRate;
            if (GameTickNum >= GameTickRate) GameTickNum = 0;
        }

        public void PreDraw()
        {
            if (SpeedMultiplier <= 0) Fraction = 0;
            var snd = new List<VMEntity>(SoundEntities);
            foreach (var sound in snd)
                sound.TickSounds();
            //fractional animation for avatars
            foreach (var obj in Entities)
            {
                (obj as VMAvatar)?.FractionalAnim(Fraction);
            }
        }

        public void SendCommand(VMNetCommandBodyAbstract cmd)
        {
            cmd.ActorUID = MyUID;
            Driver.SendCommand(cmd);
        }

        public void SendDirectCommand(uint targetID, VMNetCommandBodyAbstract cmd)
        {
            //clients can't directly message each other - it must go through the server (no p2p). Only server drivers can use this.
            Driver.SendDirectCommand(targetID, cmd);
        }

        public void ForwardCommand(VMNetCommandBodyAbstract cmd)
        {
            Driver.SendCommand(cmd);
        }

        public string GetUserIP(uint uid)
        {
            if (uid == MyUID) return "local";
            return Driver.GetUserIP(uid);
        }

        public void CloseNet(VMCloseNetReason reason)
        {
            if (reason == VMCloseNetReason.LeaveLot && !Ready) return;
            Driver.CloseReason = reason;
            Driver.Shutdown();
        }

        public void ReplaceNet(VMNetDriver driver)
        {
            lock (Driver)
            {
                Driver = driver;
            }
        }

        public void Tick()
        {
            if (FSOVAsyncLoading) return;
            if (BHAVDirty)
            {
                foreach (var ent in Entities)
                {
                    if (ent.Thread != null)
                    {
                        foreach (var frame in ent.Thread.Stack)
                            if (frame.Routine.Chunk.RuntimeVer != frame.Routine.RuntimeVer) frame.CodeOwner.Resource.Recache();
                    }
                }
                BHAVDirty = false;
            }

            lock (Driver)
            {
                if (Driver.Tick(this)) //returns true the first time we catch up to the state.
                    Ready = true;
            }
        }

        public void InternalTick(uint tickID)
        {
            Scheduler.BeginTick(tickID);
            if (GlobalLink != null) GlobalLink.Tick(this);
            if (EODHost != null) EODHost.Tick();
            if (SpeedMultiplier > 0)
            {
                var lastHour = Context.Clock.Hours;
                Context.Clock.Tick();

                if (lastHour != Context.Clock.Hours && Context.Clock.Hours % 6 == 0)
                {
                    ProcessQTRDay();
                }
            }

            Context.Architecture.Tick();

            if (SpeedMultiplier < 0) return;
            if (!UseSchedule) { //scheduleless mode is still useful for desync debug.
                var entCpy = Entities.ToArray();
                foreach (var obj in entCpy)
                {
                    //Context.NextRandom(1);
                    obj.Tick(); //run object specific tick behaviors, like lockout count decrement
                    if (obj.Thread != null && Trace != null) {
                        foreach (var item in obj.Thread.Stack)
                        {
                            Context.NextRandom(1);
                            if (item is VMRoutingFrame)
                            {
                                Trace.Trace(obj.ObjectID + "("+Context.RandomSeed+"): "+"VMRoutingFrame with state: "+ ((VMRoutingFrame)item).State.ToString());
                            }
                            else
                            {
                                var opcode = item.GetCurrentInstruction().Opcode;
                                var primitive = (opcode > 255) ? null : VMContext.Primitives[opcode];
                                Trace.Trace(obj.ObjectID + "(" + Context.RandomSeed + "): " + item.Routine.Rti.Name.TrimEnd('\0')+':'+item.InstructionPointer+" ("+ ((primitive == null) ? opcode.ToString() : primitive.Name) + ")");
                            }
                        }
                    }
                }
            } else
            {
                Scheduler.RunTick();
            }

            if (tickID % Math.Max(1, SpeedMultiplier) == 0) Context.ProcessLightingChanges();
            //Context.SetToNextCache.VerifyPositions(); use only for debug!
        }

        public void ProcessQTRDay()
        {
            foreach (var ent in Entities)
            {
                (ent.PlatformState as VMIObjectState)?.ProcessQTRDay(this, ent);
            }
        }

        public void UpdateTuning()
        {
            TuningCache.UpdateTuning(this);
            Context.InitSpecialTuning();
        }

        /// <summary>
        /// Adds an entity to this Virtual Machine.
        /// </summary>
        /// <param name="entity">The entity to add.</param>
        public void AddEntity(VMEntity entity)
        {
            entity.ObjectID = ObjectId;
            ObjectsById.Add(entity.ObjectID, entity);
            AddToObjList(this.Entities, entity);
            if (!entity.GhostImage) Context.ObjectQueries.NewObject(entity);
            ObjectId = NextObjID();
        }

        public static void AddToObjList(List<VMEntity> list, VMEntity entity)
        {
            if (list.Count == 0) { list.Add(entity); return; }
            int id = entity.ObjectID;
            int max = list.Count;
            int min = 0;
            while (max>min)
            {
                int mid = (max+min) / 2;
                int nid = list[mid].ObjectID;
                if (id < nid) max = mid;
                else if (id == nid) return; //do not add dupes
                else min = mid+1;
            }
            list.Insert(min, entity);
            // list.Insert((list[min].ObjectID>id)?min:((list[max].ObjectID > id)?max:max+1), entity);
        }

        public static void DeleteFromObjList(List<VMEntity> list, VMEntity entity)
        {
            if (list.Count == 0) { return; }
            int id = entity.ObjectID;
            int max = list.Count;
            int min = 0;
            while (max > min)
            {
                int mid = (max + min) / 2;
                int nid = list[mid].ObjectID;
                if (id < nid) max = mid;
                else if (id == nid)
                {
                    list.RemoveAt(mid); //found it
                    return;
                }
                else min = mid + 1;
            }
            //list.RemoveAt(min);
        }

        public static int FindNextIndexInObjList(List<VMEntity> list, short targId)
        {
            if (list.Count == 0) return 0;
            int count = list.Count;
            int max = count;
            int min = 0;
            while (max > min)
            {
                int mid = (max + min) / 2;
                int nid = list[mid].ObjectID;
                if (targId < nid) max = mid; //target object is below us
                else if (targId == nid)
                {
                    //found it. find NEXT!
                    return mid+1;
                }
                else min = mid + 1; //target object is above us
            }
            if (min >= count) return count;
            return list[min].ObjectID > targId ? min : min+1;
        }

        /// <summary>
        /// Removes an entity from this Virtual Machine.
        /// </summary>
        /// <param name="entity">The entity to remove.</param>
        public void RemoveEntity(VMEntity entity)
        {
            if (Entities.Contains(entity))
            {
                Context.ObjectQueries.RemoveObject(entity);
                DeleteFromObjList(Entities, entity);
                ObjectsById.Remove(entity.ObjectID);
                Scheduler.DescheduleTick(entity);
                if (entity.ObjectID < ObjectId) ObjectId = entity.ObjectID; //this id is now the smallest free object id.
            }
            entity.Dead = true;
        }

        /// <summary>
        /// Finds the next free object ID and remembers it for use when making another object.
        /// </summary>
        private short NextObjID()
        {
            for (short i = ObjectId; i > 0; i++)
                if (!ObjectsById.ContainsKey(i)) return i;
            return 0;
        }

        /// <summary>
        /// Gets a global value set for this Virtual Machine.
        /// </summary>
        /// <param name="var">The index of the global value to get. WARNING: Throws exception if index is OOB.
        /// Must be in range of 0 - 31.</param>
        /// <returns>A global value if found.</returns>
        public short GetGlobalValue(ushort var)
        {
            // should this be in VMContext?
            if (var >= GlobalState.Length) throw new Exception("Global Access out of bounds!");
            
            switch (var)
            {
                case 0:
                    return (short)Context.Clock.Hours;
                case 1:
                    return (short)Context.Clock.DayOfMonth;
                case 4:
                    return (short)Context.Clock.TimeOfDay;
                case 5:
                    return (short)Context.Clock.Minutes;
                case 6:
                    return (short)Context.Clock.Seconds;
                case 7:
                    return (short)Context.Clock.Month;
                case 8:
                    return (short)Context.Clock.Year;
            }
            return GlobalState[var];
        }

        /// <summary>
        /// Sets a global value for this Virtual Machine.
        /// </summary>
        /// <param name="var">Index for value, must be in range 0 - 31.</param>
        /// <param name="value">Global value.</param>
        /// <returns>True if successful. WARNING: If index was OOB, exception is thrown.</returns>
        public bool SetGlobalValue(ushort var, short value)
        {
            if (var >= GlobalState.Length) throw new Exception("Global Access out of bounds!");
            GlobalState[var] = value;
            return true;
        }
        private static event VMBHAVChangeDelegate OnBHAVChange;

        public static void BHAVChanged(BHAV bhav)
        {
            OnBHAVChange?.Invoke();
        }

        /// <summary>
        /// Signals a Dialog to all listeners. (usually a UI)
        /// </summary>
        /// <param name="info">The dialog info to pass along.</param>
        public void SignalDialog(VMDialogInfo info)
        {
            if (Driver.InResync) return;
            OnDialog?.Invoke(info);
        }

        /// <summary>
        /// Signals a chat event to all listeners. (usually a UI)
        /// </summary>
        /// <param name="info">The chat event to pass along.</param>
        public void SignalChatEvent(VMChatEvent evt)
        {
            if (Driver.InResync) return;
            OnChatEvent?.Invoke(evt);
        }

        public void SignalEODMessage(VMNetEODMessageCmd msg)
        {
            OnEODMessage?.Invoke(msg);
        }

        public void SignalLotSwitch(uint lotId)
        {
            OnRequestLotSwitch?.Invoke(lotId);
        }

        public void SignalGenericVMEvt(VMEventType type, object data)
        {
            OnGenericVMEvent?.Invoke(type, data);
        }

        public VMSandboxRestoreState Sandbox()
        {
            var state = new VMSandboxRestoreState { Entities = Entities, ObjectId = ObjectId,
                ObjectsById = ObjectsById, ObjectQueries = Context.ObjectQueries, RandomSeed = Context.RandomSeed };

            Context.ObjectQueries = new VMObjectQueries(Context);
            Entities = new List<VMEntity>();
            ObjectsById = new Dictionary<short, VMEntity>();
            ObjectId = 1;

            return state;
        }

        public void SandboxRestore(VMSandboxRestoreState state)
        {
            Entities = state.Entities;
            ObjectsById = state.ObjectsById;
            ObjectId = state.ObjectId;
            Context.ObjectQueries = state.ObjectQueries;
            Context.RandomSeed = state.RandomSeed;
        }

#region VM Marshalling Functions
        public VMMarshal Save()
        {
            var ents = new VMEntityMarshal[Entities.Count];
            var threads = new VMThreadMarshal[Entities.Count];
            var mult = new List<VMMultitileGroupMarshal>();

            int i = 0;
            foreach (var ent in Entities)
            {
                if (ent is VMAvatar)
                {
                    ents[i] = ((VMAvatar)ent).Save();
                }
                else
                {
                    ents[i] = ((VMGameObject)ent).Save();
                }
                threads[i++] = ent.Thread.Save();
                if (ent.MultitileGroup.BaseObject == ent)
                {
                    mult.Add(ent.MultitileGroup.Save());
                }
            }

            return new VMMarshal
            {
                Context = Context.Save(),
                TS1 = TS1,
                Entities = ents,
                Threads = threads,
                MultitileGroups = mult.ToArray(),
                GlobalState = (short[])GlobalState.Clone(),
                PlatformState = PlatformState,
                ObjectId = ObjectId,
                Tuning = Tuning
            };
        }

        public VMHollowMarshal HollowSave()
        {
            var ents = new List<VMHollowGameObjectMarshal>();
            var mult = new List<VMMultitileGroupMarshal>();

            foreach (var ent in Entities)
            {

                if (ent is VMGameObject && !(ent.Container != null && ent.Container is VMAvatar))
                {
                    if (ent.MultitileGroup.Objects.All(x => x.GetValue(VMStackObjectVariable.Hidden) > 0 || (!Context.RoomInfo[Context.GetRoomAt(x.Position)].Room.IsOutside))) continue;
                    //todo: recursively check if parent object is vm avatar.
                    //restoring state ignores objects with invalid containers anyways.
                    ents.Add(((VMGameObject)ent).HollowSave());
                    if (ent.MultitileGroup.BaseObject == ent)
                    {
                        mult.Add(ent.MultitileGroup.Save());
                    }
                }
            }

            return new VMHollowMarshal
            {
                Context = Context.Save(),
                Entities = ents.ToArray(),
                MultitileGroups = mult.ToArray()
            };
        }

        public bool FSOVAsyncLoading;
        public bool FSOVDoAsyncLoad;
        public bool FSOVClientJoin;
        public int FSOVObjLoaded;
        public int FSOVObjTotal;

        public void Load(VMMarshal input)
        {
            FSOVClientJoin = (Context.Architecture == null);
            FSOVAsyncLoading = true;
            LoadAsync(input);
            LoadComplete();
        }

        public void LoadAsync(VMMarshal input)
        {
            FSOVAsyncLoading = true;
            var lastBp = Context.Blueprint; //try keep this alive I suppose
            //var oldWorld = Context.World;
            //TS1 = input.TS1;
            Context = new VMContext(input.Context, Context);
            Context.VM = this;
            var idMap = input.Context.Architecture.IDMap;
            if (idMap != null) idMap.Apply(this);
            Context.Architecture.RegenRoomMap();
            Context.RegeneratePortalInfo();
            Context.Architecture.Terrain.RegenerateCenters();

            if (VM.UseWorld)
            {
                Context.Blueprint.Altitude = Context.Architecture.Terrain.Heights;
                Context.Blueprint.AltitudeCenters = Context.Architecture.Terrain.Centers;
            }

            var oldSounds = new List<VMSoundTransfer>();

            if (Entities != null) //free any object resources here.
            {
                foreach (var obj in Entities)
                {
                    obj.Dead = true;
                    if (obj.HeadlineRenderer != null)
                    {
                        if (UseWorld)
                        {
                            GameThread.InUpdate(() =>
                            {
                                obj.HeadlineRenderer.Dispose();
                            });
                        }
                    }
                    oldSounds.AddRange(obj.GetActiveSounds());
                }
            }

            SoundEntities = new HashSet<VMEntity>();
            Entities = new List<VMEntity>();
            Scheduler.Reset();
            ObjectsById = new Dictionary<short, VMEntity>();
            FSOVObjTotal = input.Entities.Length;
            foreach (var ent in input.Entities)
            {
                VMEntity realEnt;
                var objDefinition = FSO.Content.Content.Get().WorldObjects.Get(ent.GUID);
                if (objDefinition == null)
                {
                    LoadErrors.Add(new VMLoadError(VMLoadErrorCode.MISSING_OBJECT, 
                        ent.GUID.ToString("x8") + " " + input.MultitileGroups.FirstOrDefault()?.Name ?? "(unknown name)", (ushort)ent.ObjectID));
                    ent.LoadFailed = true;
                    continue;
                }
                if (ent is VMAvatarMarshal)
                {
                    var avatar = new VMAvatar(objDefinition);
                    avatar.Load((VMAvatarMarshal)ent);
                    if (UseWorld) Context.Blueprint.AddAvatar((AvatarComponent)avatar.WorldUI);
                    realEnt = avatar;
                }
                else
                {
                    var worldObject = Context.MakeObjectComponent(objDefinition);
                    var obj = new VMGameObject(objDefinition, worldObject);
                    obj.Load((VMGameObjectMarshal)ent);
                    if (UseWorld)
                    {
                        Context.Blueprint.AddObject((ObjectComponent)obj.WorldUI);
                        Context.Blueprint.ChangeObjectLocation((ObjectComponent)obj.WorldUI, obj.Position);
                    }
                    obj.Position = obj.Position;
                    realEnt = obj;
                }
                realEnt.FetchTreeByName(Context);
                Entities.Add(realEnt);
                Context.ObjectQueries.NewObject(realEnt);
                ObjectsById.Add(ent.ObjectID, realEnt);
                FSOVObjLoaded++;
            }

            int i = 0;
            foreach (var ent in input.Entities)
            {
                if (ent.LoadFailed)
                {
                    i++;
                    continue;
                }
                var threadMarsh = input.Threads[i];
                var realEnt = Entities[i++];

                realEnt.Thread = new VMThread(threadMarsh, Context, realEnt);
                Scheduler.ScheduleTickIn(realEnt, 1);

                if (realEnt is VMAvatar)
                    ((VMAvatar)realEnt).LoadCrossRef((VMAvatarMarshal)ent, Context);
                else
                    ((VMGameObject)realEnt).LoadCrossRef((VMGameObjectMarshal)ent, Context);
            }

            foreach (var multi in input.MultitileGroups)
            {
                var grp = new VMMultitileGroup(multi, Context); //should self register
                if (VM.UseWorld)
                {
                    var b = grp.BaseObject;
                    var avgPos = new LotTilePos();
                    foreach (var obj in grp.Objects)
                    {
                        avgPos += obj.Position;
                    }
                    avgPos /= grp.Objects.Count;

                    foreach (var obj in grp.Objects)
                    {
                        var off = obj.Position - avgPos;
                        obj.WorldUI.MTOffset = new Vector3(off.x, off.y, 0);
                        obj.Position = obj.Position;
                    }
                }
                var persist = grp.BaseObject?.PersistID ?? 0;
                if (persist != 0 && grp.BaseObject is VMGameObject) Context.ObjectQueries.RegisterMultitilePersist(grp, persist);
            }

            foreach (var ent in Entities)
            {
                if (ent.Container == null) ent.PositionChange(Context, true); //called recursively for contained objects.
            }

            GlobalState = input.GlobalState;
            if (TS1)
            {
                ((VMTS1LotState)input.PlatformState).CurrentFamily = TS1State.CurrentFamily;
            }
            var lastPlatformState = PlatformState;
            PlatformState = input.PlatformState;
            PlatformState.ActivateValidator(this);
            if (lastPlatformState != null && lastPlatformState is VMTSOLotState)
            {
                TSOState.Names = ((VMTSOLotState)lastPlatformState).Names;
            }
            ObjectId = input.ObjectId;

            //just a few final changes to refresh everything, and avoid signalling objects
            var clock = Context.Clock;
            Context.Architecture.SetTimeOfDay();

            Context.Architecture.SignalAllDirty();
            Context.DisableRouteInvalidation = true;
            Context.Architecture.Tick();
            Context.DisableRouteInvalidation = false;

            Context.Architecture.WallDirtyState(input.Context.Architecture);

            if (oldSounds.Count > 0)
            {
                GameThread.InUpdate(() =>
                {
                    foreach (var snd in oldSounds)
                    {
                        //find new owners
                        var obj = GetObjectById(snd.SourceID);
                        if (obj == null || obj.Object.GUID != snd.SourceGUID) snd.SFX.Sound.RemoveOwner(snd.SourceID);
                        else
                        {
                            SoundEntities.Add(obj);
                            obj.SoundThreads.Add(snd.SFX); // successfully transfer sound to new object
                        }
                    }
                });
            }
            
            Context.UpdateTSOBuildableArea();
            Tuning = input.Tuning;
            UpdateTuning();
            if (OnFullRefresh != null) OnFullRefresh();
        }

        public void LoadComplete()
        {
            FSOVAsyncLoading = false;
            Context.RefreshAllLighting(); //height of some objects not loaded during async load - must regenerate lighting for the rooms.
            if (FSOVClientJoin)
            {
                //run clientJoin functions to play object sounds, update some gfx.
                foreach (var obj in Entities)
                {
                    obj.ExecuteEntryPoint(30, Context, true);
                }
            }
        }

        public void HollowLoad(VMHollowMarshal input)
        {
            var clientJoin = (Context.Architecture == null);
            var oldWorld = Context.World;
            input.Context.Ambience.ActiveBits = 0;
            Context = new VMContext(input.Context, Context);
            Context.VM = this;
            Context.Architecture.RegenRoomMap();
            Context.RegeneratePortalInfo();

            Entities = new List<VMEntity>();
            ObjectsById = new Dictionary<short, VMEntity>();
            var includedEnts = new List<VMHollowGameObjectMarshal>();
            foreach (var ent in input.Entities)
            {
                VMEntity realEnt;
                var objDefinition = FSO.Content.Content.Get().WorldObjects.Get(ent.GUID);

                var worldObject = Context.MakeObjectComponent(objDefinition);
                var obj = new VMGameObject(objDefinition, worldObject);
                obj.HollowLoad(ent);
                if (UseWorld)
                {
                    Context.Blueprint.AddObject((ObjectComponent)obj.WorldUI);
                    Context.Blueprint.ChangeObjectLocation((ObjectComponent)obj.WorldUI, obj.Position);
                }
                obj.Position = obj.Position;
                realEnt = obj;

                includedEnts.Add(ent);
                Entities.Add(realEnt);
                Context.ObjectQueries.NewObject(realEnt);
                ObjectsById.Add(ent.ObjectID, realEnt);
            }

            int i = 0;
            foreach (var realEnt in Entities)
            {
                var ent = includedEnts[i++];
                ((VMGameObject)realEnt).LoadHollowCrossRef(ent, Context);
            }

            foreach (var multi in input.MultitileGroups)
            {
                new VMMultitileGroup(multi, Context); //should self register
            }

            foreach (var ent in Entities)
            {
                if (ent.Container == null) ent.PositionChange(Context, true); //called recursively for contained objects.
            }

            input.Context.Architecture.WallsDirty = true;
            input.Context.Architecture.FloorsDirty = true;
            Context.Architecture.WallDirtyState(input.Context.Architecture);
            Context.Architecture.Tick();
        }

        internal void BreakpointHit(VMEntity entity)
        {
            if (OnBreakpoint == null) entity.Thread.ThreadBreak = VMThreadBreakMode.Active; //no handler..
            else OnBreakpoint(entity);
        }

        public void ListenBHAVChanges()
        {
            OnBHAVChange -= VM_OnBHAVChange;
        }

        public void SuppressBHAVChanges()
        {
            OnBHAVChange -= VM_OnBHAVChange;
        }
#endregion
    }

    public delegate void VMBHAVChangeDelegate();

    public class VMSandboxRestoreState
    {
        public List<VMEntity> Entities;
        public Dictionary<short, VMEntity> ObjectsById;
        public short ObjectId = 1;
        public VMObjectQueries ObjectQueries;
        public ulong RandomSeed;
    }

    public enum VMEventType
    {
        TSOUnignore,
        TSOTimeout,
        TS1LotChange,
        TS1BuildBuyChange
    }
}
