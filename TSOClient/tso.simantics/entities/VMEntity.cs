/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.LotView;
using FSO.LotView.Components;
using FSO.LotView.Model;
using FSO.Content;
using FSO.Content.Model;
using FSO.Files.Formats.IFF.Chunks;
using FSO.HIT;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model;
using FSO.SimAntics.Model.Routing;
using FSO.SimAntics.Marshals.Threads;
using FSO.SimAntics.Marshals;
using FSO.Common.Utils;
using FSO.SimAntics.Model.TSOPlatform;
using FSO.SimAntics.Model.Sound;
using FSO.SimAntics.Primitives;
using FSO.LotView.Utils;
using FSO.LotView.RC;
using System.Linq;
using FSO.LotView.Utils.Camera;
using FSO.SimAntics.NetPlay.Model;

namespace FSO.SimAntics
{
    public class VMEntityRTTI
    {
        public string[] AttributeLabels;
    }

    /// <summary>
    /// An object in the VM (Virtual Machine)
    /// </summary>
    public abstract class VMEntity
    {
        public static Func<VMEntity, Texture2D> MissingIconProvider;
        public static bool UseWorld = true;

        public VMEntityRTTI RTTI;
        public bool GhostImage;
        public VMMultitileGroup IgnoreIntersection; //Ignore collisions/slots from any of these objects.

        //own properties (for instance)
        public short ObjectID;
        public uint PersistID;
        public VMPlatformState PlatformState;
        public VMTSOEntityState TSOState
        {
            get
            {
                return (PlatformState != null && PlatformState is VMTSOEntityState) ? (VMTSOEntityState)PlatformState : null;
            }
        }

        public short[] ObjectData;
        public LinkedList<short> MyList = new LinkedList<short>();
        public List<VMSoundEntry> SoundThreads;

        public VMRuntimeHeadline Headline;
        public VMHeadlineRenderer HeadlineRenderer; //IS NOT serialized, but rather regenerated on deserialize.

        public GameObject Object;
        public VMThread Thread;
        public VMMultitileGroup MultitileGroup;

        public short MainParam; //parameters passed to main on creation.
        public short MainStackOBJ;

        public VMEntity[] Contained = new VMEntity[0];
        public VMEntity Container;
        public short ContainerSlot;
        public bool Dead; //set when the entity is removed, threads owned by this object or with this object as callee will be cancelled/have their stack emptied.

        /** Persistent state variables controlled by bhavs **/
        //in TS1, NumAttributes can be 0 and it will dynamically resize as required.
        //for backwards compatability, we support this.
        private List<short> Attributes;

        public virtual short GetAttribute(int index)
        {
            while (index >= Attributes.Count) return 0;
            return Attributes[index];
        }

        public virtual void SetAttribute(int index, short value)
        {
            while (index >= Attributes.Count) Attributes.Add(0);
            Attributes[index] = value;
        }

        /** Relationship variables **/
        public Dictionary<ushort, List<short>> MeToObject;
        public Dictionary<uint, List<short>> MeToPersist;
        //a runtime cache for objects that have relationships to us. Used to get a quick reference to objects
        //that may need to delete a relationship to us.
        //note this can point to false positives, but the worst case is a slow deletion if somehow every object is added.
        public HashSet<ushort> MayHaveRelToMe = new HashSet<ushort>();

        //signals which relationships have changed since the last time this was reset
        //used to partial update relationships when doing an avatar save to db
        public HashSet<uint> ChangedRels = new HashSet<uint>();

        public ulong DynamicSpriteFlags; /** Used to show/hide dynamic sprites **/
        public ulong DynamicSpriteFlags2;
        public VMEntityObstacle Footprint;
        public bool StaticFootprint = true;

        private LotTilePos _Position = new LotTilePos(LotTilePos.OUT_OF_WORLD);
        public EntityComponent WorldUI;

        public uint TimestampLockoutCount = 0;
        public Color LightColor = Color.White;

        //inferred properties (from object resource)
        public GameGlobalResource SemiGlobal;
        public TTAB TreeTable;
        public TTAs TreeTableStrings;
        public Dictionary<string, VMTreeByNameTableEntry> TreeByName;
        public SLOT Slots;
        public OBJD MasterDefinition; //if this object is multitile, its master definition will be stored here.
        public OBJfFunctionEntry[] EntryPoints;  /** Entry points for specific events, eg. init, main, clean... **/
        public VMEntityTuning TuningReplacement;
        public bool Portal => EntryPoints[15].ActionFunction != 0;
        public bool Window => ((VMEntityFlags2)GetValue(VMStackObjectVariable.FlagField2)).HasFlag(VMEntityFlags2.ArchitectualWindow);
        public virtual bool MovesOften
        {
            get
            {
                if (Container != null)
                {
                    if (Container is VMAvatar) return true;
                    return Container.MovesOften;
                }
                if (Slots != null && Slots.Slots.ContainsKey(3))
                {
                    var slots = Slots.Slots[3];
                    if (slots.Count > 20) return true;
                }
                return MovedSelf;
            }
        }
        public bool MovedSelf;

        public string Name
        {
            get
            {
                if (MultitileGroup.Name != "") return MultitileGroup.Name;
                else return this.ToString();
            }
            set
            {
                MultitileGroup.Name = value;
            }
        }

        private bool DynamicMultitile
        {
            get
            {
                return EntryPoints[8].ActionFunction >= 256;
            }
        }

        public override string ToString()
        {
            if (MultitileGroup.Name != "") return MultitileGroup.Name;
            var strings = Object.Resource.Get<CTSS>(Object.OBJ.CatalogStringsID);
            if (strings != null)
            {
                return strings.GetString(0);
            }
            var label = Object.OBJ.ChunkLabel;
            if (label != null && label.Length > 0)
            {
                return label;
            }
            return Object.OBJ.GUID.ToString("X");
        }

        //positioning properties

        protected static Direction[] DirectionNotches = new Direction[]
        {
            Direction.NORTH,
            Direction.NORTHEAST,
            Direction.EAST,
            Direction.SOUTHEAST,
            Direction.SOUTH,
            Direction.SOUTHWEST,
            Direction.WEST,
            Direction.NORTHWEST
        };

        public LotTilePos Position
        {
            get { return _Position; }
            set
            {
                _Position = value;
                if (UseWorld) WorldUI.Level = Position.Level;
                if (this is VMAvatar) ((VMAvatar)this).VisualPositionStart = null;
                VisualPosition = new Vector3(_Position.x / 16.0f, _Position.y / 16.0f, (_Position.Level - 1) * 2.95f);
            }
        }

        public abstract Vector3 VisualPosition { get; set; }
        public abstract FSO.LotView.Model.Direction Direction { get; set; }
        public abstract float RadianDirection { get; set; }

        /// <summary>
        /// Constructs a new VMEntity instance.
        /// </summary>
        /// <param name="obj">A GameObject instance.</param>
        public VMEntity(GameObject obj)
        {
            this.Object = obj;
            ObjectData = new short[80];
            MeToObject = new Dictionary<ushort, List<short>>();
            MeToPersist = new Dictionary<uint, List<short>>();
            SoundThreads = new List<VMSoundEntry>();

            RTTI = new VMEntityRTTI();
            var numAttributes = obj.OBJ.NumAttributes;

            if (obj.OBJ.UsesFnTable == 0) EntryPoints = GenerateFunctionTable(obj.OBJ);
            else
            {
                var OBJfChunk = obj.Resource.Get<OBJf>(obj.OBJ.ChunkID); //objf has same id as objd
                if (OBJfChunk != null) EntryPoints = OBJfChunk.functions;
            }

            if (obj.GUID == 0xa9bb3a76) EntryPoints[17] = new OBJfFunctionEntry(); 

            var test = obj.Resource.List<OBJf>();

            SemiGlobal = obj.Resource.SemiGlobal;

            Slots = obj.Resource.Get<SLOT>(obj.OBJ.SlotID); //containment slots are dealt with in the avatar and object classes respectively.

            var attributeTable = obj.Resource.Get<STR>(256);
            if (attributeTable != null)
            {
                numAttributes = (ushort)Math.Max(numAttributes, attributeTable.Length);
                RTTI.AttributeLabels = new string[numAttributes];
                for (var i = 0; i < numAttributes; i++)
                {
                    RTTI.AttributeLabels[i] = attributeTable.GetString(i);
                }
            }

            TreeTable = obj.Resource.Get<TTAB>(obj.OBJ.TreeTableID);
            if (TreeTable != null) TreeTableStrings = obj.Resource.Get<TTAs>(obj.OBJ.TreeTableID);
            if (TreeTable == null && SemiGlobal != null)
            {
                TreeTable = SemiGlobal.Get<TTAB>(obj.OBJ.TreeTableID); //tree not in local, try semiglobal
                TreeTableStrings = SemiGlobal.Get<TTAs>(obj.OBJ.TreeTableID);
            }
            //TODO: global interactions like salvage

            this.Attributes = new List<short>(numAttributes);
            SetFlag(VMEntityFlags.ChairFacing, true);
        }

        public void ResetData()
        {
            ObjectData = new short[80];
            this.Attributes = new List<short>(Object.OBJ.NumAttributes);
        }

        /// <summary>
        /// Supply a game object who's tree table this VMEntity can use.
        /// See: TSO.Files.formats.iff.chunks.TTAB
        /// </summary>
        /// <param name="obj">GameObject instance with a tree table to use.</param>
        public void UseTreeTableOf(GameObject obj) //manually set the tree table for an object. Used for multitile objects, which inherit this from the master.
        {
            if (TreeTable != null) return;
            var GLOBChunks = obj.Resource.List<GLOB>();
            GameGlobal SemiGlobal = null;

            if (GLOBChunks != null && GLOBChunks[0].Name != "") SemiGlobal = FSO.Content.Content.Get().WorldObjectGlobals.Get(GLOBChunks[0].Name);

            TreeTable = obj.Resource.Get<TTAB>(obj.OBJ.TreeTableID);
            if (TreeTable != null) TreeTableStrings = obj.Resource.Get<TTAs>(obj.OBJ.TreeTableID);
            if (TreeTable == null && SemiGlobal != null)
            {
                TreeTable = SemiGlobal.Resource.Get<TTAB>(obj.OBJ.TreeTableID); //tree not in local, try semiglobal
                TreeTableStrings = SemiGlobal.Resource.Get<TTAs>(obj.OBJ.TreeTableID);
            }
        }

        public void UseSemiGlobalTTAB(string sgFile, ushort id)
        {
            GameGlobal obj = FSO.Content.Content.Get().WorldObjectGlobals.Get(sgFile);
            if (obj == null) return;

            TreeTable = obj.Resource.Get<TTAB>(id);
            if (TreeTable != null) TreeTableStrings = obj.Resource.Get<TTAs>(id);
        }

        public virtual void Tick()
        {
            if (Thread != null)
            {
                Thread.ScheduleIdleEnd = 0;
                Thread.TicksThisFrame = 0;
                Thread.Tick();
                if (Thread.ScheduleIdleEnd == 0 && !Dead)
                {
                    Thread.Context.VM.Scheduler.ScheduleTickIn(this, 1);
                }
            }
            if (Headline != null)
            {
                var over = HeadlineRenderer?.Update() ?? true;
                if (over)
                {
                    HeadlineRenderer?.Dispose();
                    Headline = null;
                    HeadlineRenderer = null;
                }
                else if (UseWorld)
                {
                    WorldUI.Headline = HeadlineRenderer.DrawFrame(Thread.Context.World);
                }
            }
            if (UseWorld && Headline == null && WorldUI.Headline != null)
            {
                WorldUI.Headline = null;
            }
        }

        public void TickSounds()
        {
            if (!UseWorld) return;
            if (Dead)
            {
                if (Thread != null) Thread.Context.VM.SoundEntities.Remove(this);
                return;
            }
            if (Thread != null)
            {
                var worldState = Thread.Context.World.State;
                var worldSpace = worldState.WorldSpace;
                var scrPos = WorldUI.GetScreenPos(worldState);
                scrPos -= new Vector2(worldSpace.WorldPxWidth/2, worldSpace.WorldPxHeight/2);
                for (int i = 0; i < SoundThreads.Count; i++)
                {
                    var sound = SoundThreads[i].Sound;
                    if (sound.Dead)
                    {
                        SoundThreads.RemoveAt(i--);
                        if (SoundThreads.Count == 0) Thread.Context.VM.SoundEntities.Remove(this);
                        continue;
                    }

                    float pan = (SoundThreads[i].Pan) ? Math.Max(-1.0f, Math.Min(1.0f, scrPos.X / worldSpace.WorldPxWidth)) : 0;
                    pan = pan * pan * ((pan > 0)?1:-1);


                    float volume = 1f;
                    
                    var rcs = worldState.Cameras.ActiveCamera as CameraController3D;
                    if (rcs != null)
                    {
                        var vp = VisualPosition * 3f;
                        var delta = rcs.Camera.Target - new Vector3(vp.X, vp.Z, vp.Y);
                        delta.Z /= 3f;
                        //volume = 4f / delta.Length();
                        volume = 1.5f - delta.Length() / 40f;
                        volume *= (10 / ((rcs.Zoom3D * rcs.Zoom3D) + 10));
                        volume *= worldState.PreciseZoom;
                    }
                    else
                    {
                        volume = (SoundThreads[i].Pan) ? 1 - (float)Math.Max(0, Math.Min(1, Math.Sqrt(scrPos.X * scrPos.X + scrPos.Y * scrPos.Y) / worldSpace.WorldPxWidth)) : 1;
                        volume *= worldState.PreciseZoom;

                        if (SoundThreads[i].Zoom) volume /= 4 - (int)worldState.Zoom;
                    }
                    if (Position.Level > worldState.Level) volume /= 4;
                    else if (Position.Level != worldState.Level) volume /= 2;

                    volume = Math.Min(1f, Math.Max(0f, volume));

                    if (sound.SetVolume(volume, pan, ObjectID))
                    {
                        if (this is VMAvatar && sound is HITThread) this.SubmitHITVars((HITThread)sound);
                        /*
                         * TODO: 3d sound
                        if (Thread.Context.World is LotView.RC.WorldRC)
                        {
                            //3d sound
                            //sound.Set3D(new Vector3(VisualPosition.X, VisualPosition.Z, VisualPosition.Y) * 3f);
                        }
                        */
                    }
                }
            }
        }

        public virtual void SubmitHITVars(HITThread thread)
        {
            if (thread.ObjectVar == null) return;
            var attr = new List<int>();// { 0 };
            attr.Add(0);
            attr.AddRange(Attributes.Select(x => (int)x));
            thread.ObjectVar = attr.ToArray();
        }

        public List<VMSoundTransfer> GetActiveSounds()
        {
            var result = new List<VMSoundTransfer>();
            foreach (var snd in SoundThreads)
            {
                result.Add(new VMSoundTransfer(ObjectID, Object.OBJ.GUID, snd));
            }
            return result;
        }

        public bool RunEveryFrame()
        {
            return (this is VMAvatar || (Headline != null) || ((VMGameObject)this).Disabled > VMGameObjectDisableFlags.ForSale);
        }

        public OBJfFunctionEntry[] GenerateFunctionTable(OBJD obj)
        {
            OBJfFunctionEntry[] result = new OBJfFunctionEntry[33];

            result[0].ActionFunction = obj.BHAV_Init;
            result[1].ActionFunction = obj.BHAV_MainID;
            result[2].ActionFunction = obj.BHAV_Load;
            result[3].ActionFunction = obj.BHAV_Cleanup;
            result[4].ActionFunction = obj.BHAV_QueueSkipped;
            result[5].ActionFunction = obj.BHAV_AllowIntersectionID;
            result[6].ActionFunction = obj.BHAV_WallAdjacencyChanged;
            result[7].ActionFunction = obj.BHAV_RoomChange;
            result[8].ActionFunction = obj.BHAV_DynamicMultiTileUpdate;
            result[9].ActionFunction = obj.BHAV_Place;
            result[10].ActionFunction = obj.BHAV_Pickup;
            result[11].ActionFunction = obj.BHAV_UserPlace;
            result[12].ActionFunction = obj.BHAV_UserPickup;
            result[13].ActionFunction = obj.BHAV_LevelInfo;
            result[14].ActionFunction = obj.BHAV_ServingSurface;
            result[15].ActionFunction = obj.BHAV_Portal; //portal
            result[16].ActionFunction = obj.BHAV_GardeningID;
            result[17].ActionFunction = obj.BHAV_WashHandsID;
            result[18].ActionFunction = obj.BHAV_PrepareFoodID;
            result[19].ActionFunction = obj.BHAV_CookFoodID;
            result[20].ActionFunction = obj.BHAV_PlaceSurfaceID;
            result[21].ActionFunction = obj.BHAV_DisposeID;
            result[22].ActionFunction = obj.BHAV_EatID;
            result[23].ActionFunction = obj.BHAV_PickupFromSlotID; //pickup from slot
            result[24].ActionFunction = obj.BHAV_WashDishID;
            result[25].ActionFunction = obj.BHAV_EatSurfaceID;
            result[26].ActionFunction = obj.BHAV_SitID;
            result[27].ActionFunction = obj.BHAV_StandID;
            result[28].ActionFunction = obj.BHAV_Clean;
            result[29].ActionFunction = obj.BHAV_Repair; //repair
            result[30].ActionFunction = 0; //client house join
            result[31].ActionFunction = 0; //prepare for sale
            result[32].ActionFunction = 0; //house unload

            return result;
        }

        public virtual void Init(VMContext context)
        {
            FetchTreeByName(context);
            if (!GhostImage)
            {
                this.Thread = new VMThread(context, this, this.Object.OBJ.StackSize);
                context.VM.Scheduler.ScheduleTickIn(this, 1);
            }

            ExecuteEntryPoint(0, context, true); //Init
            ExecuteEntryPoint(8, context, true, null, new short[] { 0, 0, 0, 0 }); //dynamic multitile - say we don't have any adjacent objects to start

            if (!GhostImage)
            {
                short[] Args = null;
                VMEntity StackOBJ = null;
                if (MainParam != 0)
                {
                    Args = new short[4];
                    Args[0] = MainParam;
                    MainParam = 0;
                }
                if (MainStackOBJ != 0)
                {
                    StackOBJ = context.VM.GetObjectById(MainStackOBJ);
                    MainStackOBJ = 0;
                }

                ExecuteEntryPoint(1, context, false, StackOBJ, Args); //Main
            }
            else
            {
                SetValue(VMStackObjectVariable.Room, -1);
                if (this is VMGameObject) ((VMGameObject)this).RefreshLight();
            }

            UpdateTuning(context.VM);
        }

        private bool InReset = false;
        public virtual void Reset(VMContext context)
        {
            if (InReset) return;
            InReset = true;

            //if an exception happens here, it will have to be fatal.

            if (this.Thread == null) return;
            this.Thread.Stack.Clear();
            this.Thread.Queue.Clear();
            Thread.QueueDirty = true;
            this.Thread.ActiveQueueBlock = -1;
            this.Thread.BlockingState = null;
            this.Thread.EODConnection = null;

            if (EntryPoints[3].ActionFunction != 0 && ((this is VMGameObject) || ((VMAvatar)this).IsPet)) ExecuteEntryPoint(3, context, true); //Reset
            if (!GhostImage) ExecuteEntryPoint(1, context, false); //Main

            if (this is VMGameObject)
            {
                context.VM.Scheduler.DescheduleTick(this);
                context.VM.Scheduler.ScheduleTickIn(this, 1);
            }

            InReset = false;
        }

        public void FetchTreeByName(VMContext context)
        {
            TreeByName = Object.Resource.TreeByName;
        }

        public bool ExecuteEntryPoint(int entry, VMContext context, bool runImmediately)
        {
            return ExecuteEntryPoint(entry, context, runImmediately, null, null);
        }

        public bool ExecuteEntryPoint(int entry, VMContext context, bool runImmediately, VMEntity stackOBJ)
        {
            return ExecuteEntryPoint(entry, context, runImmediately, stackOBJ, null);
        }

        public bool ExecuteGenericEntryPoint(OBJfFunctionEntry entry, VMContext context, bool runImmediately, VMEntity stackOBJ, short[] args)
        {
            if (entry.ActionFunction > 255)
            {
                bool result = false;
                VMSandboxRestoreState SandboxState = null;
                if (GhostImage && runImmediately)
                {
                    SandboxState = context.VM.Sandbox();
                    for (int i = 0; i < MultitileGroup.Objects.Count; i++)
                    {
                        var obj = MultitileGroup.Objects[i];
                        context.VM.AddEntity(obj);
                    }
                }

                ushort ActionID = entry.ActionFunction;
                var tree = GetRoutineWithOwner(ActionID, context);

                if (tree != null)
                {
                    var routine = tree.routine;
                    var frame = new VMStackFrame
                    {
                        Caller = this,
                        Callee = this,
                        CodeOwner = tree.owner,
                        Routine = routine,
                        StackObject = stackOBJ,
                        Args = args
                    };

                    if (runImmediately)
                    {
                        var checkResult = VMThread.EvaluateCheck(context, this, frame);
                        result = (checkResult == VMPrimitiveExitCode.RETURN_TRUE);
                    }
                    else
                    {
                        Thread.Push(frame);
                    }
                }

                if (GhostImage && runImmediately)
                {
                    //restore state
                    context.VM.SandboxRestore(SandboxState);
                }
                return result;
            }
            else
            {
                return false;
            }
        }

        public bool ExecuteEntryPoint(int entry, VMContext context, bool runImmediately, VMEntity stackOBJ, short[] args)
        {
            if (args == null) args = new short[4];
            if (entry == 11)
            {
                //user placement, hack to do auto floor removal/placement for stairs
                if (Object.OBJ.LevelOffset > 0 && Position != LotTilePos.OUT_OF_WORLD)
                {
                    var floor = context.Architecture.GetFloor(Position.TileX, Position.TileY, Position.Level);
                    var placeFlags = (VMPlacementFlags)ObjectData[(int)VMStackObjectVariable.PlacementFlags];
                    if ((placeFlags & VMPlacementFlags.InAir) > 0)
                        context.Architecture.SetFloor(Position.TileX, Position.TileY, Position.Level, new FloorTile(), true);
                    if ((placeFlags & VMPlacementFlags.OnFloor) > 0 && (floor.Pattern == 0))
                        context.Architecture.SetFloor(Position.TileX, Position.TileY, Position.Level, new FloorTile { Pattern = 1 } , true);
                }
            }

            bool result = false;
            if (entry < EntryPoints.Length)
            {
                return ExecuteGenericEntryPoint(EntryPoints[entry], context, runImmediately, stackOBJ, args);
            } else
            {
                return false;
            }
        }

        public bool ExecuteNamedEntryPoint(string name, VMContext context, bool runImmediately, VMEntity stackOBJ, short[] args)
        {
            VMTreeByNameTableEntry tree;
            if (TreeByName.TryGetValue(name, out tree))
            {
                var routine = (VMRoutine)tree.bhav;
                return ExecuteGenericEntryPoint(new OBJfFunctionEntry() { ActionFunction = routine.ID }, context, runImmediately, stackOBJ, args);
            }
            return false;
        }

        public VMBHAVOwnerPair GetRoutineWithOwner(ushort ActionID, VMContext context)
        {
            VMRoutine routine = null;
            GameObject CodeOwner;
            if (ActionID < 4096)
            { //global
                routine = (VMRoutine)context.Globals.Resource.GetRoutine(ActionID);
            }
            else if (ActionID < 8192)
            { //local
                routine = (VMRoutine)Object.Resource.GetRoutine(ActionID);
            }
            else
            { //semi-global
                if (SemiGlobal != null)
                    routine = (VMRoutine)SemiGlobal.GetRoutine(ActionID);
            }

            CodeOwner = Object;

            if (routine == null) return null;
            return new VMBHAVOwnerPair(routine, CodeOwner);
        }

        public VMBHAVOwnerPair GetBHAVWithOwner(ushort ActionID, VMContext context)
        {
            BHAV bhav = null;
            GameObject CodeOwner;
            if (ActionID < 4096)
            { //global
                bhav = context.Globals.Resource.Get<BHAV>(ActionID);
            }
            else if (ActionID < 8192)
            { //local
                bhav = Object.Resource.Get<BHAV>(ActionID);
            }
            else
            { //semi-global
                if (SemiGlobal != null)
                    bhav = SemiGlobal.Get<BHAV>(ActionID);
            }

            CodeOwner = Object;

            if (bhav == null) return null;
            return new VMBHAVOwnerPair(bhav, CodeOwner);

        }

        public bool IsDynamicSpriteFlagSet(ushort index)
        {
            return (index > 63)? ((DynamicSpriteFlags2 & ((ulong)0x1 << (index-64))) > 0) :
                (DynamicSpriteFlags & ((ulong)0x1 << index)) > 0;
        }

        public virtual void SetDynamicSpriteFlag(ushort index, bool set)
        {
            if (set) {
                if (index > 63)
                {
                    ulong bitflag = ((ulong)0x1 << (index-64));
                    DynamicSpriteFlags2 = DynamicSpriteFlags2 | bitflag;
                } else {
                    ulong bitflag = ((ulong)0x1 << index);
                    DynamicSpriteFlags = DynamicSpriteFlags | bitflag;
                }
            } else {
                if (index > 63)
                {
                    DynamicSpriteFlags2 = DynamicSpriteFlags2 & (~((ulong)0x1 << (index-64)));
                }
                else
                {
                    DynamicSpriteFlags = DynamicSpriteFlags & (~((ulong)0x1 << index));
                }
            }
        }

        public void UpdateTuning(VM vm)
        {
            TuningReplacement = new VMEntityTuning(this, vm);
        }

        public void UpdateFootprint()
        {
            var current = Footprint;
            Footprint = GetObstacle(Position, Direction, false);
            if (current != Footprint)
            {
                current?.Unregister();

                if (Footprint != null)
                {
                    if (current != null)
                    {
                        //add this footprint to the set the Current had
                        Footprint.Set = current.Set;
                        Footprint.Dynamic = current.Dynamic;

                        if (StaticFootprint) Footprint.Set.Add(Footprint);
                        else Footprint.Dynamic.Add(Footprint);
                    } else
                    {
                        //new footprint started existing. To add this to the room we need the VMContext...
                        Thread?.Context?.AddFootprint(Footprint);
                    }
                }
            }
        }

        public void SetFlag(VMEntityFlags flag, bool set)
        {
            if (set) ObjectData[(int)VMStackObjectVariable.Flags] |= (short)(flag);
            else ObjectData[(int)VMStackObjectVariable.Flags] &= ((short)~(flag));

            if (flag == VMEntityFlags.HasZeroExtent) UpdateFootprint();
            return;
        }

        public bool GetFlag(VMEntityFlags flag)
        {
            return ((VMEntityFlags)ObjectData[(int)VMStackObjectVariable.Flags] & flag) > 0;
        }

        public virtual short GetValue(VMStackObjectVariable var)
        {
            switch (var) //special cases
            {
                case VMStackObjectVariable.Room:
                    if (Position == LotTilePos.OUT_OF_WORLD) return -5;
                    break;
                case VMStackObjectVariable.ObjectId:
                    return ObjectID;
                case VMStackObjectVariable.Direction:
                    return (short)((Math.Round((RadianDirection / Math.PI) * 4) + 8) % 8);
                case VMStackObjectVariable.ContainerId:
                case VMStackObjectVariable.ParentId: //TODO: different?
                    return (Container == null) ? (short)0 : Container.ObjectID;
                case VMStackObjectVariable.SlotNumber:
                    return (Container == null) ? (short)-1 : ContainerSlot;
                case VMStackObjectVariable.SlotCount:
                    return (short)TotalSlots();
                case VMStackObjectVariable.UseCount:
                    //TS1 Person to Person: use count is checked to see if both avatars still have the interaction in their queues.
                    //it doesn't have to be the active entity to count towards the use count.
                    return (short)((Thread == null)?0:GetUsers(Thread.Context, null).Count);
                case VMStackObjectVariable.LockoutCount:
                    var count = ObjectData[(short)var];
                    if (TimestampLockoutCount > Thread.Context.VM.Scheduler.CurrentTickID) TimestampLockoutCount = 0; //lockout counts in the future are invalid.
                    return (short)((Thread == null) ? count : Math.Max((long)count - (Thread.Context.VM.Scheduler.CurrentTickID - TimestampLockoutCount), 0));
                case VMStackObjectVariable.CurrentValue:
                    return (short)MultitileGroup.InitialPrice;
            }
            if ((short)var > 79) throw new Exception("Object Data out of range!");
            return ObjectData[(short)var];
        }

        public virtual bool SetValue(VMStackObjectVariable var, short value)
        {
            switch (var) //special cases
            {
                case VMStackObjectVariable.Flags:
                    var old = ObjectData[(short)var];
                    ObjectData[(short)var] = value;
                    if (((value ^ old) & (int)VMEntityFlags.HasZeroExtent) > 0)
                        UpdateFootprint();
                    if (((value ^ old) & (int)VMEntityFlags.FSODynamicFootprint) != 0)
                        StaticFootprint = (value & (int)VMEntityFlags.FSODynamicFootprint) == 0;
                    if (this is VMAvatar && ((value ^ old) & (int)VMEntityFlags.Burning) > 0)
                        this.Reset(Thread.Context);
                    return true;
                case VMStackObjectVariable.Direction:
                    value = (short)(((int)value + 65536) % 8);
                    Direction = DirectionNotches[value];
                    break;
                case VMStackObjectVariable.Hidden:
                    if (UseWorld) WorldUI.Visible = value == 0;
                    break;
                case VMStackObjectVariable.LockoutCount:
                    if (Thread != null) TimestampLockoutCount = Thread.Context.VM.Scheduler.CurrentTickID;
                    break;
                case VMStackObjectVariable.FSOEngineQuery:
                    //write a query to this variable and a result will be written to it.
                    switch (value)
                    {
                        case 1: //safe to delete
                            value = (short)((IsInUse(Thread.Context, true, true) || (Container != null && Container is VMAvatar)) ? 0 : 1);
                            break;
                    }
                    break;
                case VMStackObjectVariable.Category:
                    if (Thread != null)
                    {
                        Thread.Context.ObjectQueries.RemoveCategory(this, ObjectData[(short)var]);
                        Thread.Context.ObjectQueries.RegisterCategory(this, value);
                    }
                    break;
                case VMStackObjectVariable.CurrentValue:
                    MultitileGroup.InitialPrice = value;
                    break;
            }

            if ((short)var > 79) throw new Exception("Object Data out of range!");
            ObjectData[(short)var] = value;
            return true;

        }

        // Begin Container SLOTs interface

        public abstract int TotalSlots();
        public abstract bool PlaceInSlot(VMEntity obj, int slot, bool cleanOld, VMContext context);
        public abstract VMEntity GetSlot(int slot);
        public abstract void ClearSlot(int slot);
        public abstract int GetSlotHeight(int slot);

        public void RecurseSlotPositionChange(VMContext context, bool noEntryPoint)
        {
            context.UnregisterObjectPos(this, true);
            var total = TotalSlots();
            for (int i=0; i<total; i++)
            {
                var obj = GetSlot(i);
                if (obj != null) obj.RecurseSlotPositionChange(context, noEntryPoint);
            }
            Position = Position;
            PositionChange(context, noEntryPoint);
        }

        public void RecurseSlotFunc(Action<VMEntity> func)
        {
            func(this);
            var count = TotalSlots();

            for (int i = 0; i < count; i++)
            {
                var item = GetSlot(i);
                if (item != null)
                {
                    item.RecurseSlotFunc(func);
                }
            }
        }

        public bool WillLoopSlot(VMEntity test)
        {
            if (test == this) return true;
            if (Container != null)
            {
                return Container.WillLoopSlot(test);
            } else
            {
                return false;
            }
        }

        // End Container SLOTs interface

        public virtual void SetRoom(ushort room)
        {
            SetValue(VMStackObjectVariable.Room, (short)room);
        }

        public List<VMPieMenuInteraction> GetPieMenuForInteraction(VM vm, VMEntity caller, int index, bool global, bool includeHidden)
        {
            TTABInteraction ia;
            TTAs ttas = TreeTableStrings;
            if (index < 0) return null;
            if (!global)
            {
                if (TreeTable == null || !TreeTable.InteractionByIndex.TryGetValue((uint)index, out ia)) return null;
            }
            else
            {
                if (!vm.Context.GlobalTreeTable.InteractionByIndex.TryGetValue((uint)index, out ia)) return null;
                ttas = vm.Context.GlobalTTAs;
            }
            var id = ia.TTAIndex;
            var action = GetAction((int)id, caller, vm.Context, global);

            caller.ObjectData[(int)VMStackObjectVariable.HideInteraction] = 0;
            var actionStrings = caller.Thread.CheckAction(action);
            if ((caller.ObjectData[(int)VMStackObjectVariable.Hidden] == 1 ||
                caller.ObjectData[(int)VMStackObjectVariable.HideInteraction] == 1 ||
                caller.Position == LotTilePos.OUT_OF_WORLD) && !includeHidden) return null;

            if (actionStrings != null)
            {
                if (actionStrings.Count > 0)
                {
                    foreach (var actionS in actionStrings)
                    {
                        if (actionS.Name != null) actionS.Name = ttas?.GetString((int)id) ?? "***MISSING***";
                        actionS.ID = (byte)id;
                        actionS.Entry = ia;
                        actionS.Global = global;
                    }
                }
                else
                {
                    if (ttas != null)
                    {
                        actionStrings.Add(new VMPieMenuInteraction()
                        {
                            Name = ttas.GetString((int)id),
                            ID = (byte)id,
                            Entry = ia,
                            Global = global
                        });
                    }
                }
            }

            return actionStrings;
        }

        public List<VMPieMenuInteraction> GetPieMenu(VM vm, VMEntity caller, bool includeHidden, bool includeGlobal)
        {
            var pie = new List<VMPieMenuInteraction>();
            if (TreeTable == null) return pie;

            var myLength = TreeTable.Interactions.Length;
            var globalLength = vm.Context.GlobalTreeTable?.Interactions.Length ?? 0;

            for (int i = 0; i < myLength+globalLength; i++)
            {
                TTABInteraction ia;
                TTAs ttas = TreeTableStrings;
                bool global = false;
                if (i < myLength)
                {
                    ia = TreeTable.Interactions[i];
                } else
                {
                    if (!includeGlobal) break;
                    ia = vm.Context.GlobalTreeTable.Interactions[i - myLength];
                    ttas = vm.Context.GlobalTTAs;
                    global = true;
                }
                var id = ia.TTAIndex;
                var action = GetAction((int)id, caller, vm.Context, global);

                caller.ObjectData[(int)VMStackObjectVariable.HideInteraction] = 0;
                var actionStrings = caller.Thread.CheckAction(action);
                if ((caller.ObjectData[(int)VMStackObjectVariable.Hidden] == 1 ||
                    caller.ObjectData[(int)VMStackObjectVariable.HideInteraction] == 1 ||
                    caller.Position == LotTilePos.OUT_OF_WORLD) && !includeHidden) continue;

                if (actionStrings != null)
                {
                    if (actionStrings.Count > 0)
                    {
                        foreach (var actionS in actionStrings)
                        {
                            if (actionS.Name == null) actionS.Name = ttas?.GetString((int)id) ?? "***MISSING***";
                            actionS.ID = (byte)id;
                            actionS.Entry = ia;
                            actionS.Global = global;
                            pie.Add(actionS);
                        }
                    }
                    else
                    {
                        if (ttas != null)
                        {
                            pie.Add(new VMPieMenuInteraction()
                            {
                                Name = ttas.GetString((int)id),
                                ID = (byte)id,
                                Entry = ia,
                                Global = global
                            });
                        }
                    }
                }
            }

            return pie;
        }
        
        public VMQueuedAction GetAction(int interaction, VMEntity caller, VMContext context, bool global)
        {
            return GetAction(interaction, caller, context, global, null);
        }

        public VMQueuedAction GetAction(int interaction, VMEntity caller, VMContext context, bool global, short[] args)
        {
            var ttab = global ? context.GlobalTreeTable : TreeTable;
            var ttas = global ? context.GlobalTTAs : TreeTableStrings;
            if (ttab?.InteractionByIndex?.ContainsKey((uint)interaction) != true) return null;
            var Action = ttab.InteractionByIndex[(uint)interaction];

            ushort actionID = Action.ActionFunction;
            var aTree = GetRoutineWithOwner(actionID, context);
            if (aTree == null) return null;
            var aRoutine = aTree.routine;

            VMRoutine cRoutine = null;
            ushort checkID = Action.TestFunction;
            if (checkID != 0)
            {
                var cTree = GetRoutineWithOwner(checkID, context);
                if (cTree == null) return null; // VERIFIED: TS1 ignores interactions with non-zero missing check trees. Fixes downtown clerk.
                cRoutine = cTree.routine;
            }

            if (global) interaction |= unchecked((int)0x80000000);

            return new VMQueuedAction
            {
                Callee = this,
                IconOwner = this,
                CodeOwner = aTree.owner,
                ActionRoutine = aRoutine,
                CheckRoutine = cRoutine,
                Name = (ttas==null)?"":ttas.GetString((int)Action.TTAIndex),
                StackObject = this,
                Args = args,
                InteractionNumber = interaction,
                Priority = (short)VMQueuePriority.UserDriven,
                Flags = Action.Flags,
                Flags2 = Action.Flags2,
            };
        }

        public void PushUserInteraction(int interaction, VMEntity caller, VMContext context, bool global)
        {
            PushUserInteraction(interaction, caller, context, global, null);
        }

        public void PushUserInteraction(int interaction, VMEntity caller, VMContext context, bool global, short[] args)
        {
            var action = GetAction(interaction, caller, context, global, args);
            if (action != null) caller.Thread.EnqueueAction(action);
        }

        private VMPlacementError IndividualUserMovable(VMContext context, bool deleting)
        {
            if (this is VMAvatar) return VMPlacementError.CantBePickedup;
            var movementFlags = (VMMovementFlags)GetValue(VMStackObjectVariable.MovementFlags);
            if ((movementFlags & VMMovementFlags.PlayersCanMove) == 0) return VMPlacementError.CantBePickedup;
            if (deleting && (movementFlags & VMMovementFlags.PlayersCanDelete) == 0) return VMPlacementError.ObjectNotOwnedByYou;
            if (context.IsUserOutOfBounds(Position)) return VMPlacementError.CantBePickedupOutOfBounds;
            if (IsInUse(context, true)) return VMPlacementError.InUse;
            var total = TotalSlots();
            for (int i = 0; i < total; i++)
            {
                var item = GetSlot(i);
                if (item != null &&
                    (deleting || item is VMAvatar || item.IsUserMovable(context, deleting) != VMPlacementError.Success)) return VMPlacementError.CantBePickedup;
            }
            return VMPlacementError.Success;
        }

        public VMPlacementError IsUserMovable(VMContext context, bool deleting)
        {
            if (context.Cheats.MoveObjects) // for ts1 moveobjects cheat
                return VMPlacementError.Success;
            foreach (var obj in MultitileGroup.Objects)
            {
                var result = obj.IndividualUserMovable(context, deleting);
                if (result != VMPlacementError.Success) return result;
            }
            return VMPlacementError.Success;
        }

        public HashSet<VMEntity> GetUsers(VMContext context, HashSet<VMEntity> users)
        {
            if (users == null)
            {
                users = new HashSet<VMEntity>();
                foreach (var obj in MultitileGroup.Objects)
                {
                    obj.GetUsers(context, users);
                }
            }
            else
            {
                foreach (var ava in context.ObjectQueries.Avatars)
                {
                    if (ava == this) continue;
                    bool found = false;
                    for (int i = 0; i <= ava.Thread.ActiveQueueBlock; i++)
                    {
                        var item = ava.Thread.Queue[i];
                        if (item.Callee == this)
                        {
                            found = true;
                            break;
                        }
                    }

                    // TSO/TS1 expects the use count of the 2 sim interaction object to be zero until both sims are actively using it.
                    // if it detects the use count is greater than zero before this point, then it will attempt to update the "person count"
                    // since neither sim has started the interaction, the relationships to them do not exist, and the person count is set to zero
                    // this causes interactions to early exit most of the time... so only count active actions. 

                    if (found) users.Add(ava);
                }
            }
            return users;
        }

        public bool IsInUse(VMContext context, bool multitile)
        {
            return IsInUse(context, multitile, false);
        }

        public bool IsInUse(VMContext context, bool multitile, bool stackObjSafety)
        {
            if (multitile)
            {
                foreach (var obj in MultitileGroup.Objects)
                {
                    if (obj.IsInUse(context, false)) return true;
                }
            }
            else
            {
                if (GetFlag(VMEntityFlags.Occupied)) return true;
                foreach (var ava in context.ObjectQueries.Avatars)
                {
                    foreach (var item in ava.Thread.Stack)
                    {
                        if (item.Callee == this) return true;
                        if (stackObjSafety && item.StackObject == this) return true;
                    }
                }
            }
            return false;
        }

        public VMPlacementResult PositionValid(LotTilePos pos, Direction direction, VMContext context, VMPlaceRequestFlags flags)
        {
            if (pos == LotTilePos.OUT_OF_WORLD) return new VMPlacementResult();
            var userOOB = context.IsUserOutOfBounds(pos);
            if ((((flags & VMPlaceRequestFlags.UserBuildableLimit) > 0) && userOOB) || context.IsOutOfBounds(pos))
                return new VMPlacementResult(VMPlacementError.LocationOutOfBounds);

            //TODO: speedup with exit early checks
            //TODO: corner checks (wtf uses this)

            var arch = context.Architecture;
            var wall = arch.GetWall(pos.TileX, pos.TileY, pos.Level); //todo: preprocess to check which walls are real solid walls and not fences.

            if (this is VMGameObject) //needs special handling for avatar eventually
            {
                VMPlacementError wallValid = WallChangeValid(wall, direction, true);
                if (wallValid != VMPlacementError.Success) return new VMPlacementResult(wallValid);
            }

            var floor = arch.GetPreciseFloor(pos);
            VMPlacementError floorValid = FloorChangeValid(floor, pos.Level);
            if (floorValid != VMPlacementError.Success) return new VMPlacementResult(floorValid);

            if (!userOOB && arch.GetTerrainSloped(pos.TileX, pos.TileY)) //sloped
            {
                var slopeValid = SlopeValid();
                if (slopeValid != VMPlacementError.Success) return new VMPlacementResult(slopeValid);
            }

            //we've passed the wall test, now check if we intersect any objects.
            if ((flags & VMPlaceRequestFlags.AllowIntersection) > 0) return new VMPlacementResult(VMPlacementError.Success);
            var oldRadDir = RadianDirection;
            Direction = direction; //allow intersect tree may need the new direction
            var valid = (this is VMAvatar) ? context.GetAvatarPlace(this, pos, direction, flags) : context.GetObjPlace(this, pos, direction, flags);
            RadianDirection = oldRadDir;
            if (valid.Object != null && ((flags & VMPlaceRequestFlags.AcceptSlots) == 0))
                valid.Status = VMPlacementError.CantIntersectOtherObjects;
            return valid;
        }

        public VMPlacementError SlopeValid()
        {
            if (this is VMAvatar) return VMPlacementError.Success;
            var placeFlags = (VMPlacementFlags)ObjectData[(int)VMStackObjectVariable.PlacementFlags];

            if ((placeFlags & VMPlacementFlags.OnFloor) > 0 && (placeFlags & VMPlacementFlags.OnSlope) == 0)
            {
                return VMPlacementError.CantPlaceOnSlope;
            }

            return VMPlacementError.Success;
        }

        public VMPlacementError FloorChangeValid(ushort floor, sbyte level)
        {
            var placeFlags = (VMPlacementFlags)ObjectData[(int)VMStackObjectVariable.PlacementFlags];

            if (floor == 65535)
            {
                if ((placeFlags & (VMPlacementFlags.AllowOnPool | VMPlacementFlags.RequirePool)) == 0) return VMPlacementError.CantPlaceOnWater;
            }
            else
            {
                if ((placeFlags & VMPlacementFlags.RequirePool) > 0) return VMPlacementError.MustPlaceOnPool;
                if (floor == 65534)
                {
                    if ((placeFlags & (VMPlacementFlags.OnWater | VMPlacementFlags.RequireWater)) == 0) return VMPlacementError.CantPlaceOnWater;
                } else
                {
                    if ((placeFlags & VMPlacementFlags.RequireWater) > 0) return VMPlacementError.MustPlaceOnWater;
                    if (floor == 0)
                    {
                        if (level == 1)
                        {
                            if ((placeFlags & VMPlacementFlags.OnTerrain) == 0) return VMPlacementError.NotAllowedOnTerrain;
                        }
                        else
                        {
                            if ((placeFlags & VMPlacementFlags.InAir) == 0 && Object.OBJ.LevelOffset == 0) return VMPlacementError.CantPlaceInAir;
                            //TODO: special hack check that determines if we need can add/remove a tile to fulfil this if LevelOffset > 0
                        }
                    }
                    else
                    {
                        if (this is VMGameObject && (placeFlags & VMPlacementFlags.OnFloor) == 0 && 
                            ((Object.OBJ.LevelOffset == 0) || (placeFlags & VMPlacementFlags.InAir) == 0)) return VMPlacementError.NotAllowedOnFloor;
                    }
                }
            }
            return VMPlacementError.Success;
        }

        public VMPlacementError WallChangeValid(WallTile wall, Direction direction, bool checkUnused)
        {
            var placeFlags = (WallPlacementFlags)ObjectData[(int)VMStackObjectVariable.WallPlacementFlags];

            bool diag = ((wall.Segments & (WallSegments.HorizontalDiag | WallSegments.VerticalDiag)) > 0);
            if (diag && (placeFlags & WallPlacementFlags.DiagonalAllowed) == 0) return VMPlacementError.CantBeThroughWall; //does not allow diagonal and one is present
            else if (!diag && ((placeFlags & WallPlacementFlags.DiagonalRequired) > 0)) return VMPlacementError.MustBeOnDiagonal; //needs diagonal and one is not present

            int rotate = (DirectionToWallOff(direction) + 1) % 4;
            int rotPart = RotateWallSegs(wall.Segments, rotate);
            int useRotPart = RotateWallSegs(wall.OccupiedWalls, rotate);

            if (((int)placeFlags & rotPart) != ((int)placeFlags & 15)) return VMPlacementError.MustBeAgainstWall; //walls required are not there in this configuration

            //walls that we are attaching to must not be in use!
            var exclusive = GetValue(VMStackObjectVariable.ExclusivePlacementFlags);
            if ((exclusive & 2) > 0 && checkUnused && ((int)placeFlags & useRotPart) > 0) return VMPlacementError.MustBeAgainstUnusedWall;

            if (((int)placeFlags & (rotPart << 8)) > 0) return VMPlacementError.CantBeThroughWall; //walls not allowed are there in this configuration
            
            return VMPlacementError.Success;
        }

        internal int RotateWallSegs(WallSegments ws, int rotate) {
            int wallSides = (int)ws;
            int rotPart = ((wallSides << (4 - rotate)) & 15) | ((wallSides & 15) >> rotate);
            return rotPart;
        }

        internal void SetWallUse(VMArchitecture arch, bool set, bool exclusive)
        {
            var wall = arch.GetWall(Position.TileX, Position.TileY, Position.Level);

            var placeFlags = (WallPlacementFlags)ObjectData[(int)VMStackObjectVariable.WallPlacementFlags];
            int rotate = (8 - (DirectionToWallOff(Direction) + 1)) % 4;
            byte rotPart = (byte)RotateWallSegs((WallSegments)((int)placeFlags % 15), rotate);

            if (EntryPoints[6].ActionFunction != 0)
            {
                var mainSegs = (WallSegments)RotateWallSegs(wall.Segments, (4 - rotate) % 4);
                var wallAdj = (VMWallAdjacencyFlags)0; //wall adjacency uses a weird bit order. TODO: Wall in front of left/right (used by stairs)
                if ((mainSegs & WallSegments.TopLeft) > 0) wallAdj |= VMWallAdjacencyFlags.WallInFront;
                if ((mainSegs & WallSegments.TopRight) > 0) wallAdj |= VMWallAdjacencyFlags.WallOnRight;
                if ((mainSegs & WallSegments.BottomRight) > 0) wallAdj |= VMWallAdjacencyFlags.WallBehind;
                if ((mainSegs & WallSegments.BottomLeft) > 0) wallAdj |= VMWallAdjacencyFlags.WallOnLeft;

                SetValue(VMStackObjectVariable.WallAdjacencyFlags, (short)wallAdj);

                if (Thread != null) ExecuteEntryPoint(6, Thread.Context, true, this);
            }

            if (rotPart == 0) return;

            if (exclusive)
            {
                if (UseWorld) ((ObjectComponent)WorldUI).AdjacentWall = (WallSegments)rotPart;

                if (set) wall.OccupiedWalls |= (WallSegments)rotPart;
                else wall.OccupiedWalls &= (WallSegments)~rotPart;

                arch.SetWall(Position.TileX, Position.TileY, Position.Level, wall);
            }
        }

        public void Delete(bool cleanupAll, VMContext context)
        {
            if (cleanupAll && !DynamicMultitile) MultitileGroup.Delete(context);
            else
            {
                if (Dead) return;
                
                //DEBUG
                if (Object.OBJ.GUID == 0x5157DDF2 || Object.OBJ.GUID == 0x3278BD34)
                {
                    if (context.VM.Scheduler.PendingDeletion.Count == 0 || context.VM.Scheduler.RunningNow)
                    {
                        context.VM.SignalTraceLog($"=== PET OBJECT DELETED ({ToString()} - pid: {PersistID}, owner: {(PlatformState as VMTSOObjectState)?.OwnerID ?? 0}) ===", true);
                    }
                }
                //END DEBUG

                if (context.VM.Scheduler.RunningNow)
                {
                    //queue this object to be deleted at the end of the frame
                    if (Container != null) PrePositionChange(context);
                    context.VM.Scheduler.Delete(this);
                    return;
                }
                Dead = true; //if a reset tries to delete this object it is wasting its time
                var threads = SoundThreads;

                //clear any short term relations the target object has to us
                foreach (var objID in MayHaveRelToMe)
                {
                    var obj = context.VM.GetObjectById((short)objID);
                    if (obj != null)
                        obj.MeToObject.Remove((ushort)ObjectID);
                }

                for (int i = 0; i < threads.Count; i++)
                {
                    threads[i].Sound.RemoveOwner(ObjectID);
                }
                threads.Clear();

                /*
                 * disable til ts1 behaviour reversed
                if (Container != null && Container is VMAvatar)
                {
                    //must reset our container, and any object they are using. (restaurant, TS1 behaves similarly)
                    var stack = Container.Thread.Stack;
                    var stacklast = (stack.Count == 0) ? null : stack[stack.Count - 1];
                    if (stacklast != null && stacklast.Callee != Container && stacklast.Callee != this) stacklast.Callee.Reset(context);
                    Container.Reset(context);
                }
                */

                PrePositionChange(context);
                //if we're the last object in a multitile group, and db persisted, remove us from the db.
                //this deletes all plugin data for this object too.
                if (context.VM.GlobalLink != null && PersistID >= 16777216 && MultitileGroup.Objects.Count == 1)
                    context.VM.GlobalLink.DeleteObject(context.VM, PersistID, (result) => { });
                context.RemoveObjectInstance(this);
                MultitileGroup.RemoveObject(this); //we're no longer part of the multitile group

                int slots = TotalSlots();
                for (int i = 0; i < slots; i++)
                {
                    var obj = GetSlot(i);
                    if (obj != null)
                    {
                        this.Position = obj.Position;
                        obj.SetPosition(LotTilePos.OUT_OF_WORLD, obj.Direction, context);
                        if (obj.GetValue(VMStackObjectVariable.Hidden) > 0)
                            obj.SetValue(VMStackObjectVariable.Hidden, 0); //failsafe: hidden held objects (replaced by mesh) should unhide on forced drop.
                        if (this.Position != LotTilePos.OUT_OF_WORLD) VMFindLocationFor.FindLocationFor(obj, this, context, VMPlaceRequestFlags.Default);
                    }
                }

                if (!MultitileGroup.MultiTile)
                {
                    foreach (var obj in MultitileGroup.Objects) obj.UpdateDynamicMultitileFlags(context);
                }
            }
        }

        public abstract VMEntityObstacle GetObstacle(LotTilePos pos, Direction dir, bool temp);

        public void SetObstacleStatic(bool newStatic) {
            if (newStatic != StaticFootprint)
            {
                if (Footprint != null && Footprint.Set != null)
                {
                    if (newStatic)
                    {
                        Footprint.Dynamic.Remove(Footprint);
                        Footprint.Set.Add(Footprint);
                    }
                    else
                    {
                        Footprint.Dynamic.Add(Footprint);
                        Footprint.Set.Delete(Footprint);
                    }
                }
                StaticFootprint = newStatic;
            }
        }

        public void PrePositionChange(VMContext context)
        {
            PrePositionChange(context, true);
        }

        public virtual void PrePositionChange(VMContext context, bool roomChange)
        {

        }

        public void PositionChange(VMContext context, bool noEntryPoint)
        {
            PositionChange(context, noEntryPoint, true);
        }


        public virtual void PositionChange(VMContext context, bool noEntryPoint, bool roomChange)
        {
            if (!(GhostImage || noEntryPoint)) ExecuteEntryPoint(9, context, true); //Placement
        }
        
        public VMPlacementResult SetPosition(LotTilePos pos, Direction direction, VMContext context)
        {
            return SetPosition(pos, direction, context, VMPlaceRequestFlags.Default);
        }

        public VMPlacementResult SetPosition(LotTilePos pos, Direction direction, VMContext context, VMPlaceRequestFlags flags)
        {
            if (DynamicMultitile && MultitileGroup.Objects.Count > 1)
            {
                //trying to move an individual item of a dynamic multitile object breaks it out.
                var oldGroup = MultitileGroup;
                oldGroup.RemoveObject(this);
                var newGroup = new VMMultitileGroup(MultitileGroup);
                newGroup.AddObject(this);
                MultitileGroup = newGroup;
                var result = MultitileGroup.ChangePosition(pos, direction, context, flags);

                //tell our old group
                foreach (var obj in oldGroup.Objects)
                {
                    obj.UpdateDynamicMultitileFlags(context);
                }
                return result;
            } else
            {
                return MultitileGroup.ChangePosition(pos, direction, context, flags);
            }
            
        }

        public virtual void SetIndivPosition(LotTilePos pos, Direction direction, VMContext context, VMPlacementResult info)
        {
            Direction = direction;
            if (UseWorld && this is VMGameObject) context.Blueprint.ChangeObjectLocation((ObjectComponent)WorldUI, pos);
            Position = pos;
            if (info.Object != null) info.Object.PlaceInSlot(this, 0, false, context);
        }

        internal int DirectionToWallOff(Direction dir)
        {
            switch (dir)
            {
                case Direction.NORTH:
                    return 0;
                case Direction.EAST:
                    return 1;
                case Direction.SOUTH:
                    return 2;
                case Direction.WEST:
                    return 3;
            }
            return 0;
        }

        internal void SetWallStyle(int side, VMArchitecture arch, ushort value)
        {
            //0=top right, 1=bottom right, 2=bottom left, 3 = top left
            WallTile targ;
            switch (side)
            {
                case 0:
                    targ = arch.GetWall(Position.TileX, Position.TileY, Position.Level);
                    targ.ObjSetTRStyle = value;
                    if (((VMEntityFlags2)ObjectData[(int)VMStackObjectVariable.FlagField2] & VMEntityFlags2.ArchitectualDoor) > 0) targ.TopRightDoor = value != 0;
                    arch.SetWall(Position.TileX, Position.TileY, Position.Level, targ);
                    break;
                case 1:
                    //this seems to be the rule... only set if wall is top left/right. Fixes multitile windows (like really long ones)
                    return;
                case 2:
                    return;
                case 3:
                    targ = arch.GetWall(Position.TileX, Position.TileY, Position.Level);
                    targ.ObjSetTLStyle = value;
                    if (((VMEntityFlags2)ObjectData[(int)VMStackObjectVariable.FlagField2] & VMEntityFlags2.ArchitectualDoor) > 0) targ.TopLeftDoor = value != 0;
                    arch.SetWall(Position.TileX, Position.TileY, Position.Level, targ); 
                    break;
            }
        }

        public void UpdateDynamicMultitile(VMContext context)
        {
            //check adjacent tiles for objects of this type and add them to our multitile group
            //also build adjacency flags for use by the Dynamic Multitile Update entry point
            int flags = 0;
            foreach (var ent in context.VM.Entities)
            {
                if (ent == this) continue;
                var diff = ent.Position - Position;
                if (ent.Object.OBJ.GUID == this.Object.OBJ.GUID && Math.Abs(diff.TileX) < 2 && Math.Abs(diff.TileY) < 2 && (Math.Abs(diff.TileY) + Math.Abs(diff.TileX) < 2))
                {
                    if (ent.MultitileGroup != MultitileGroup)
                    {
                        var oldObjects = ent.MultitileGroup.Objects;
                        MultitileGroup.Combine(ent.MultitileGroup);
                        foreach (var obj in oldObjects) obj.UpdateDynamicMultitile(context);
                    }

                    var direction = DirectionUtils.Normalize(Math.Atan2(ent.Position.x - Position.x, Position.y - ent.Position.y));
                    var result = (int)Math.Round((DirectionUtils.PosMod(direction, Math.PI * 2) / Math.PI) * 4);

                    var dirDiff = (int)DirectionUtils.PosMod(result - DirectionToWallOff(Direction) * 2, 8);

                    flags |= 1 << dirDiff;
                }
            }
            ExecuteEntryPoint(8, context, true, null, new short[] { (short)flags, 0, 0, 0 });
        }

        public void UpdateDynamicMultitileFlags(VMContext context)
        {
            int flags = 0;
            foreach (var ent in MultitileGroup.Objects)
            {
                if (ent == this) continue;
                var diff = ent.Position - Position;
                if (ent.Object.OBJ.GUID == this.Object.OBJ.GUID && Math.Abs(diff.TileX) < 2 && Math.Abs(diff.TileY) < 2 && (Math.Abs(diff.TileY) + Math.Abs(diff.TileX) < 2))
                {
                    var direction = DirectionUtils.Normalize(Math.Atan2(ent.Position.x - Position.x, Position.y - ent.Position.y));
                    var result = (int)Math.Round((DirectionUtils.PosMod(direction, Math.PI * 2) / Math.PI) * 4);
                    var dirDiff = (int)DirectionUtils.PosMod(result - DirectionToWallOff(Direction) * 2, 8);

                    flags |= 1 << dirDiff;
                }
            }
            ExecuteEntryPoint(8, context, true, null, new short[] { (short)flags, 0, 0, 0 });
        }

        public abstract Texture2D GetIcon(GraphicsDevice gd, int store);


        #region VM Marshalling Functions
        public void SaveEnt(VMEntityMarshal target)
        {
            var newList = new short[MyList.Count];
            int i = 0;
            foreach (var item in MyList) newList[i++] = item;

            var newContd = new short[Contained.Length];
            i = 0;
            foreach (var item in Contained) newContd[i++] = (item == null)?(short)0:item.ObjectID;

            var relArry = new VMEntityRelationshipMarshal[MeToObject.Count];
            i = 0;
            foreach (var item in MeToObject) relArry[i++] = new VMEntityRelationshipMarshal { Target = item.Key, Values = item.Value.ToArray() };

            var prelArry = new VMEntityPersistRelationshipMarshal[MeToPersist.Count];
            i = 0;
            foreach (var item in MeToPersist) prelArry[i++] = new VMEntityPersistRelationshipMarshal { Target = item.Key, Values = item.Value.ToArray() };

            target.ObjectID = ObjectID;
            target.PersistID = PersistID;
            target.PlatformState = PlatformState;
            target.ObjectData = (short[])ObjectData.Clone();
            target.MyList = newList;

            target.Headline = (Headline == null) ? null : Headline.Save();

            target.GUID = Object.OBJ.GUID;
            target.MasterGUID = (MasterDefinition == null)?0:MasterDefinition.GUID;

            target.MainParam = MainParam; //parameters passed to main on creation.
            target.MainStackOBJ = MainStackOBJ;

            target.Contained = newContd; //object ids
            target.Container = (Container == null)?(short)0:Container.ObjectID;
            target.ContainerSlot = ContainerSlot;

            target.Attributes = Attributes.ToArray();
            target.MeToObject = relArry;
            target.MeToPersist = prelArry;

            target.DynamicSpriteFlags = DynamicSpriteFlags;
            target.DynamicSpriteFlags2 = DynamicSpriteFlags2;
            target.Position = _Position;
            target.TimestampLockoutCount = TimestampLockoutCount;
            target.LightColor = LightColor;
        }

        public virtual void Load(VMEntityMarshal input)
        {
            ObjectID = input.ObjectID;
            PersistID = input.PersistID;
            PlatformState = input.PlatformState;
            ObjectData = input.ObjectData;
            MyList = new LinkedList<short>(input.MyList);

            MainParam = input.MainParam; //parameters passed to main on creation.
            MainStackOBJ = input.MainStackOBJ;

            if (input.MasterGUID != 0)
            {
                var masterDef = FSO.Content.Content.Get().WorldObjects.Get(input.MasterGUID);
                MasterDefinition = masterDef.OBJ;
                UseTreeTableOf(masterDef);
            }

            else MasterDefinition = null;

            ContainerSlot = input.ContainerSlot;

            Attributes = new List<short>(input.Attributes);
            MeToObject = new Dictionary<ushort, List<short>>();
            MeToPersist = new Dictionary<uint, List<short>>();
            foreach (var obj in input.MeToObject) MeToObject[obj.Target] = new List<short>(obj.Values);
            foreach (var obj in input.MeToPersist) MeToPersist[obj.Target] = new List<short>(obj.Values);

            DynamicSpriteFlags = input.DynamicSpriteFlags;
            DynamicSpriteFlags2 = input.DynamicSpriteFlags2;
            Position = input.Position;

            TimestampLockoutCount = input.TimestampLockoutCount;
            LightColor = input.LightColor;

            if (UseWorld) WorldUI.Visible = GetValue(VMStackObjectVariable.Hidden) == 0;
        }

        public virtual void LoadCrossRef(VMEntityMarshal input, VMContext context)
        {
            Contained = new VMEntity[input.Contained.Length];
            int i = 0;
            foreach (var item in input.Contained) Contained[i++] = context.VM.GetObjectById(item);

            Container = context.VM.GetObjectById(input.Container);
            if (UseWorld && Container != null)
            {
                WorldUI.Container = Container.WorldUI;
                WorldUI.ContainerSlot = ContainerSlot;
            }

            if (input.Headline != null)
            {
                Headline = new VMRuntimeHeadline(input.Headline, context);
                HeadlineRenderer = context.VM.Headline.Get(Headline);
            }

            var keyCopy = MeToObject.Keys.ToList();
            foreach (var objID in keyCopy)
            {
                var obj = context.VM.GetObjectById((short)objID);
                if (obj != null) obj.MayHaveRelToMe.Add((ushort)ObjectID);
                else MeToObject.Remove(objID); //cleanup refs to missing objects
            }

            UpdateTuning(context.VM);
        }
        #endregion
    }

    [Flags]
    public enum VMEntityFlags
    {
        ShowGhost = 1,
        DisallowPersonIntersection = 1 << 1,
        HasZeroExtent = 1 << 2, //4
        CanWalk = 1 << 3, //8
        AllowPersonIntersection = 1 << 4, //16
        Occupied = 1 << 5, //32
        NotifiedByIdleForInput = 1 << 6,
        InteractionCanceled = 1 << 7,
        ChairFacing = 1 << 8,
        Burning = 1 << 9,
        HideForCutaway = 1 << 10,
        FireProof = 1 << 11,
        TurnedOff = 1 << 12,
        NeedsMaintinance = 1 << 13,
        ShowDynObjNameInTooltip = 1 << 14,
        FSODynamicFootprint = 1 << 15
    }


    [Flags]
    public enum WallPlacementFlags
    {
        WallRequiredInFront = 1,
        WallRequiredOnRight = 1<<1,
        WallRequiredBehind = 1<<2,
        WallRequiredOnLeft = 1<<3,
        CornerNotAllowed = 1<<4,
        CornerRequired = 1<<5,
        DiagonalRequired = 1<<6,
        DiagonalAllowed = 1<<7,
        WallNotAllowedInFront = 1<<8,
        WallNotAllowedOnRight = 1<<9,
        WallNotAllowedBehind = 1<<10,
        WallNotAllowedOnLeft = 1<<11
    }

    [Flags]
    public enum VMPlacementFlags
    {
        OnFloor = 1,
        OnTerrain = 1 << 1,
        OnWater = 1 << 2,
        OnSurface = 1 << 3, //redundant
        OnDoor = 1 << 4, //what?
        OnWindow = 1 << 5, //curtains??
        OnLockedTile = 1 << 6, //?????
        RequireFirstLevel = 1 << 7,
        OnSlope = 1 << 8,
        InAir = 1 << 9,
        InWall = 1 << 10, //redundant?
        AllowOnPool = 1 << 11,
        RequirePool = 1 << 12,
        RequireWater = 1 << 13,
    }

    [Flags]
    public enum VMEntityFlags2
    {
        CanBreak = 1,
        CanDie = 1 << 1,
        CanBeReposessed = 1 << 2,
        ObstructsView = 1 << 3,
        Floats = 1 << 4,
        Burns = 1 << 5,
        Fixable = 1 << 6,
        CannotBeStolen = 1 << 7,
        GeneratesHeat = 1 << 8,
        CanBeLighted = 1 << 9,
        GeneratesLight = 1 << 10,
        CanGetDirty = 1 << 11,
        ContributesToAsthetic = 1 << 12,
        unused14 = 1 << 13,
        ArchitectualWindow = 1 << 14,
        ArchitectualDoor = 1 << 15
    }

    [Flags]
    public enum VMMovementFlags
    {
        SimsCanMove = 1, //only cleared by payphone...
        PlayersCanMove = 1 << 1,
        SelfPropelled = 1 << 2, //unused
        PlayersCanDelete = 1 << 3,
        StaysAfterEvict = 1 << 4
    }

    [Flags]
    public enum VMCensorshipFlags
    {
        Pelvis = 1,
        SpineIfFemale = 1 << 1,
        Head = 1 << 2,
        LeftHand = 1 << 3,
        RightHand = 1 << 4,
        LeftFoot = 1 << 5,
        RightFoot = 1 << 6,
        FullBody = 1 << 7
    }

    [Flags]
    public enum VMWallAdjacencyFlags
    {
        WallOnLeft = 1,
        WallOnRight = 1<<1,
        WallInFront = 1<<2,
        WallBehind = 1<<3,
        WallAboveLeft = 1<<4,
        WallAboveRight = 1<<5
    }
    public class VMPieMenuInteraction
    {
        public string Name;
        public short Param0;
        public byte ID;
        public TTABInteraction Entry;

        public float Score;
        public VMEntity Callee;
        public bool Global;

        public Dictionary<int, short> MotiveAdChanges;

        public override string ToString()
        {
            return (Name ?? "??") + ":" + Score;
        }
    }


}
