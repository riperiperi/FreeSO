using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using tso.world;
using tso.world.components;
using tso.world.model;
using TSO.Content;
using TSO.Content.model;
using TSO.Files.formats.iff.chunks;
using TSO.HIT;
using TSO.Simantics.engine;
using TSO.Simantics.entities;
using TSO.Simantics.model;

namespace TSO.Simantics
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
        public VMEntityRTTI RTTI;

        /** ID of the object **/
        public short ObjectID;
        public bool Cursor; //special kind of object that does not interact with the world.
        public bool Interrupt; //set to true to interrupt out of idle.

        public LinkedList<short> MyList = new LinkedList<short>();
        public Stack<StackFrame> Stack = new Stack<StackFrame>();
        public List<VMRoutine> Queue = new List<VMRoutine>();
        public GameObject Object;
        public VMThread Thread;
        public GameGlobal SemiGlobal;
        public TTAB TreeTable;
        public TTAs TreeTableStrings;
        public Dictionary<string, VMTreeByNameTableEntry> TreeByName;
        public WorldComponent WorldUI;
        public SLOT Slots;

        public short MainParam; //parameters passed to main on creation.
        public short MainStackOBJ;

        public VMMultitileGroup MultitileGroup;
        public OBJD MasterDefinition; //if this object is multitile, its master definition will be stored here.

        public List<VMSoundEntry> SoundThreads;

        public VMEntity[] Contained;
        public VMEntity Container;
        public short ContainerSlot;
        public bool Dead; //set when the entity is removed, threads owned by this object or with this object as callee will be cancelled/have their stack emptied.

        /** Persistent state variables controlled by bhavs **/
        private short[] Attributes;

        /** Relationship variables **/
        public Dictionary<ushort, Dictionary<short, short>> MeToObject;
        //todo, special system for server persistent avatars and pets

        /** Used to show/hide dynamic sprites **/
        public uint DynamicSpriteFlags;

        /** Entry points for specific events, eg. init, main, clean... **/
        public OBJfFunctionEntry[] EntryPoints;

        public short[] ObjectData;

        /// <summary>
        /// Constructs a new VMEntity instance.
        /// </summary>
        /// <param name="obj">A GameObject instance.</param>
        public VMEntity(GameObject obj)
        {
            this.Object = obj;
            /** 
             * For some reason, in the aquarium object (maybe others) the numAttributes is set to 0
             * but it should be 4. There are 4 entries in the label table. Go figure?
             */
            ObjectData = new short[80];
            MeToObject = new Dictionary<ushort, Dictionary<short, short>>();
            SoundThreads = new List<VMSoundEntry>();

            RTTI = new VMEntityRTTI();
            var numAttributes = obj.OBJ.NumAttributes;

            if (obj.OBJ.UsesFnTable == 0) EntryPoints = GenerateFunctionTable(obj.OBJ);
            else
            {
                var OBJfChunk = obj.Resource.Get<OBJf>(obj.OBJ.ChunkID); //objf has same id as objd
                if (OBJfChunk != null) EntryPoints = OBJfChunk.functions;
            }

            var test = obj.Resource.List<OBJf>();

            var GLOBChunks = obj.Resource.List<GLOB>();
            if (GLOBChunks != null)
            {
                SemiGlobal = TSO.Content.Content.Get().WorldObjectGlobals.Get(GLOBChunks[0].Name);
                Object.Resource.SemiGlobal = SemiGlobal.Resource; //used for tuning constant fetching.
            }

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
                TreeTable = SemiGlobal.Resource.Get<TTAB>(obj.OBJ.TreeTableID); //tree not in local, try semiglobal
                TreeTableStrings = SemiGlobal.Resource.Get<TTAs>(obj.OBJ.TreeTableID);
            }
            //no you cannot get global tree tables don't even ask

            this.Attributes = new short[numAttributes];
            SetFlag(VMEntityFlags.ChairFacing, true);
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

            if (GLOBChunks != null) SemiGlobal = TSO.Content.Content.Get().WorldObjectGlobals.Get(GLOBChunks[0].Name);

            TreeTable = obj.Resource.Get<TTAB>(obj.OBJ.TreeTableID);
            if (TreeTable != null) TreeTableStrings = obj.Resource.Get<TTAs>(obj.OBJ.TreeTableID);
            if (TreeTable == null && SemiGlobal != null)
            {
                TreeTable = SemiGlobal.Resource.Get<TTAB>(obj.OBJ.TreeTableID); //tree not in local, try semiglobal
                TreeTableStrings = SemiGlobal.Resource.Get<TTAs>(obj.OBJ.TreeTableID);
            }
        }

        public virtual void Tick()
        {
            //decrement lockout count
            if (ObjectData[(int)VMStackObjectVariable.LockoutCount] > 0) ObjectData[(int)VMStackObjectVariable.LockoutCount]--;
            Interrupt = false;
            TickSounds();
        }

        public void TickSounds()
        {
            if (SoundThreads.Count > 0)
            {
                var scrPos = (WorldUI is ObjectComponent) ? ((ObjectComponent)WorldUI).LastScreenPos : ((AvatarComponent)WorldUI).LastScreenPos;
                scrPos -= new Vector2(512, 0);
                for (int i = 0; i < SoundThreads.Count; i++)
                {
                    if (SoundThreads[i].Thread.Dead)
                    {
                        var old = SoundThreads[i];
                        SoundThreads.RemoveAt(i--);
                        if (old.Loop)
                        {
                            var thread = HITVM.Get().PlaySoundEvent(old.Name);
                            if (thread != null)
                            {
                                var owner = this;
                                if (!thread.AlreadyOwns(owner.ObjectID)) thread.AddOwner(owner.ObjectID);

                                var entry = new VMSoundEntry()
                                {
                                    Thread = thread,
                                    Pan = old.Pan,
                                    Zoom = old.Zoom,
                                    Loop = old.Loop,
                                    Name = old.Name
                                };
                                owner.SoundThreads.Add(entry);
                            }
                        }
                        continue;
                    }

                    float pan = (SoundThreads[i].Pan) ? Math.Max(-1.0f, Math.Min(1.0f, scrPos.X / 768)) : 0;
                    float volume = (SoundThreads[i].Pan) ? 1 - (float)Math.Max(0, Math.Min(1, Math.Sqrt(scrPos.X * scrPos.X + scrPos.Y * scrPos.Y) / 768)) : 1;

                    if (SoundThreads[i].Zoom) volume /= 4 - ((WorldUI is ObjectComponent) ? ((ObjectComponent)WorldUI).LastZoomLevel : ((AvatarComponent)WorldUI).LastZoomLevel);

                    SoundThreads[i].Thread.SetVolume(volume, pan);

                }
            }
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
            result[10].ActionFunction = obj.BHAV_PickupID;
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
            result[23].ActionFunction = 0; //pickup from slot
            result[24].ActionFunction = obj.BHAV_WashDishID;
            result[25].ActionFunction = obj.BHAV_EatSurfaceID;
            result[26].ActionFunction = obj.BHAV_SitID;
            result[27].ActionFunction = obj.BHAV_StandID;
            result[28].ActionFunction = obj.BHAV_Clean;
            result[29].ActionFunction = 0; //repair
            result[30].ActionFunction = 0; //client house join
            result[31].ActionFunction = 0; //prepare for sale
            result[32].ActionFunction = 0; //house unload

            return result;
        }

        public virtual void Init(VMContext context)
        {
            GenerateTreeByName(context);
            this.Thread = new VMThread(context, this, this.Object.OBJ.StackSize);

            ExecuteEntryPoint(0, context, true); //Init
            ExecuteEntryPoint(1, context, false); //Main
        }

        public void GenerateTreeByName(VMContext context)
        {
            TreeByName = new Dictionary<string, VMTreeByNameTableEntry>();

            var bhavs = Object.Resource.List<BHAV>();
            if (bhavs != null)
            {
                foreach (var bhav in bhavs)
                {
                    string name = bhav.ChunkLabel;
                    for (var i = 0; i < name.Length; i++)
                    {
                        if (name[i] == 0)
                        {
                            name = name.Substring(0, i);
                            break;
                        }
                    }
                    TreeByName.Add(name, new VMTreeByNameTableEntry(bhav, Object.Resource));
                }
            }
            //also add semiglobals

            if (SemiGlobal != null)
            {
                bhavs = SemiGlobal.Resource.List<BHAV>();
                if (bhavs != null)
                {
                    foreach (var bhav in bhavs)
                    {
                        string name = bhav.ChunkLabel;
                        for (var i = 0; i < name.Length; i++)
                        {
                            if (name[i] == 0)
                            {
                                name = name.Substring(0, i);
                                break;
                            }
                        }
                        if (!TreeByName.ContainsKey(name)) TreeByName.Add(name, new VMTreeByNameTableEntry(bhav, Object.Resource));
                    }
                }
            }
        }

        public void ExecuteEntryPoint(int entry, VMContext context, bool runImmediately)
        {
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
            if (EntryPoints[entry].ActionFunction > 255)
            {
                BHAV bhav;
                GameIffResource CodeOwner;
                ushort ActionID = EntryPoints[entry].ActionFunction;
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
                    bhav = SemiGlobal.Resource.Get<BHAV>(ActionID);
                }

                CodeOwner = Object.Resource;

                var routine = context.VM.Assemble(bhav);
                if (bhav == null) return; //throw new Exception("Invalid BHAV call!");

                short[] Args = null;
                VMEntity StackOBJ = null;
                if (entry == 1)
                {
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
                }

                var action = new TSO.Simantics.engine.VMQueuedAction
                {
                    Callee = this,
                    CodeOwner = CodeOwner,
                    /** Main function **/
                    StackObject = StackOBJ,
                    Routine = routine,
                    Args = Args
                };

                if (runImmediately)
                    VMThread.EvaluateCheck(context, this, action);
                else
                    this.Thread.EnqueueAction(action);
            }
        }

        public VMBHAVOwnerPair GetBHAVWithOwner(ushort ActionID, VMContext context)
        {
            BHAV bhav;
            GameIffResource CodeOwner;
            if (ActionID < 4096)
            { //global
                bhav = context.Globals.Resource.Get<BHAV>(ActionID);
                //CodeOwner = context.Globals.Resource;
            }
            else if (ActionID < 8192)
            { //local
                bhav = Object.Resource.Get<BHAV>(ActionID);

            }
            else
            { //semi-global
                bhav = SemiGlobal.Resource.Get<BHAV>(ActionID);
                //CodeOwner = SemiGlobal.Resource;
            }

            CodeOwner = Object.Resource;

            if (bhav == null) throw new Exception("Invalid BHAV call!");
            return new VMBHAVOwnerPair(bhav, CodeOwner);

        }

        public bool IsDynamicSpriteFlagSet(ushort index)
        {
            return (DynamicSpriteFlags & (0x1 << index)) > 0;
        }

        public virtual void SetDynamicSpriteFlag(ushort index, bool set)
        {
            if (set) {
                uint bitflag = (uint)(0x1 << index);
                DynamicSpriteFlags = DynamicSpriteFlags | bitflag;
            } else {
                DynamicSpriteFlags = (uint)(DynamicSpriteFlags & (~(0x1 << index)));
            }
        }

        public void SetFlag(VMEntityFlags flag, bool set)
        {
            if (set) ObjectData[(int)VMStackObjectVariable.Flags] |= (short)(flag);
            else ObjectData[(int)VMStackObjectVariable.Flags] &= ((short)~(flag));
            return;
        }

        public bool GetFlag(VMEntityFlags flag)
        {
            return ((VMEntityFlags)ObjectData[(int)VMStackObjectVariable.Flags] & flag) > 0;
        }

        public virtual short GetAttribute(ushort data)
        {
            return Attributes[data];
        }

        public virtual void SetAttribute(ushort data, short value)
        {
            Attributes[data] = value;
        }

        public virtual short GetValue(VMStackObjectVariable var)
        {
            switch (var) //special cases
            {
                case VMStackObjectVariable.ObjectId:
                    return ObjectID;
                case VMStackObjectVariable.Direction:
                    switch (this.Direction)
                    {
                        case tso.world.model.Direction.WEST:
                            return 6;
                        case tso.world.model.Direction.SOUTH:
                            return 4;
                        case tso.world.model.Direction.EAST:
                            return 2;
                        case tso.world.model.Direction.NORTH:
                            return 0;
                        default:
                            return 0;
                    }
                case VMStackObjectVariable.ContainerId:
                    return (Container == null) ? (short)0 : Container.ObjectID;
                case VMStackObjectVariable.SlotNumber:
                    return (Container == null) ? (short)-1 : ContainerSlot;
            }
            if ((short)var > 79) throw new Exception("Object Data out of range!");
            return ObjectData[(short)var];
        }

        public virtual bool SetValue(VMStackObjectVariable var, short value)
        {
            switch (var) //special cases
            {
                case VMStackObjectVariable.Direction:
                    value = (short)(((int)value + 65536) % 8);
                    switch (value) {
                        case 6:
                            Direction = tso.world.model.Direction.WEST;
                            return true;
                        case 4:
                            Direction = tso.world.model.Direction.SOUTH;
                            return true;
                        case 2:
                            Direction = tso.world.model.Direction.EAST;
                            return true;
                        case 0:
                            Direction = tso.world.model.Direction.NORTH;
                            return true;
                        default:
                            return true;
                            //throw new Exception("Diagonal Set Not Implemented!");
                    }
            }

            if ((short)var > 79) throw new Exception("Object Data out of range!");
            ObjectData[(short)var] = value;
            return true;

        }

        private LotTilePos _Position = new LotTilePos(LotTilePos.OUT_OF_WORLD);

        public LotTilePos Position
        {
            get { return _Position; }
            set
            {
                _Position = value;
                for (int i = 0; i < TotalSlots(); i++)
                {
                    var obj = GetSlot(i);
                    if (obj != null) obj.Position = _Position; //TODO: is physical position the same as the slot offset position?
                }
                VisualPosition = new Vector3(_Position.x / 16.0f, _Position.y / 16.0f, (_Position.Level - 1) * 2.95f);
            }
        }

        public abstract Vector3 VisualPosition { get; set; }
        public abstract tso.world.model.Direction Direction { get; set; }
        public abstract float RadianDirection { get; set; }

        public void Execute(VMRoutine routine) {
            Queue.Add(routine);
        }

        // Begin Container SLOTs interface

        public abstract int TotalSlots();
        public abstract void PlaceInSlot(VMEntity obj, int slot);
        public abstract VMEntity GetSlot(int slot);
        public abstract void ClearSlot(int slot);
        public abstract int GetSlotHeight(int slot);

        // End Container SLOTs interface

        public List<VMPieMenuInteraction> GetPieMenu(VM vm, VMEntity caller)
        {
            var pie = new List<VMPieMenuInteraction>();
            if (TreeTable == null) return pie;

            for (int i = 0; i < TreeTable.Interactions.Length; i++)
            {
                var action = TreeTable.Interactions[i];

                bool CanRun = false;
                if (action.TestFunction != 0 && (((TTABFlags)action.Flags & TTABFlags.Debug) != TTABFlags.Debug))
                {
                    caller.ObjectData[(int)VMStackObjectVariable.HideInteraction] = 0;
                    var Behavior = GetBHAVWithOwner(action.TestFunction, vm.Context);
                    CanRun = (VMThread.EvaluateCheck(vm.Context, caller, new VMQueuedAction()
                    {
                        Callee = this,
                        CodeOwner = Behavior.owner,
                        StackObject = this,
                        Routine = vm.Assemble(Behavior.bhav),
                    }) == VMPrimitiveExitCode.RETURN_TRUE);
                    if (caller.ObjectData[(int)VMStackObjectVariable.HideInteraction] == 1) CanRun = false;
                }
                else
                {
                    CanRun = true;
                }

               

                if (CanRun) pie.Add(new VMPieMenuInteraction()
                {
                    Name = TreeTableStrings.GetString((int)action.TTAIndex),
                    ID = (byte)action.TTAIndex
                });
            }

            return pie;
        }

        public void PushUserInteraction(int interaction, VMEntity caller, VMContext context)
        {
            var Action = TreeTable.InteractionByIndex[(uint)interaction];
            ushort ActionID = Action.ActionFunction;

            var function = GetBHAVWithOwner(ActionID, context);

            VMEntity carriedObj = caller.GetSlot(0);

            var routine = context.VM.Assemble(function.bhav);
            caller.Thread.EnqueueAction(
                new TSO.Simantics.engine.VMQueuedAction
                {
                    Callee = this,
                    CodeOwner = function.owner,
                    Routine = routine,
                    Name = TreeTableStrings.GetString((int)Action.TTAIndex),
                    StackObject = this,
                    Args = ((Action.MaskFlags & InteractionMaskFlags.AvailableWhenCarrying) > 0)
                        ? new short[] { (carriedObj == null)?(short)0:carriedObj.ObjectID, 0, 0, 0 }:null,
                    InteractionNumber = interaction,
                    Priority = VMQueuePriority.UserDriven
                }
            );
        }

        public VMPlacementResult PositionValid(LotTilePos pos, Direction direction, VMContext context)
        {
            if (pos == LotTilePos.OUT_OF_WORLD) return new VMPlacementResult();
            else if (context.IsOutOfBounds(pos)) return new VMPlacementResult { Status = VMPlacementError.LocationOutOfBounds };

            //TODO: speedup with exit early checks
            //TODO: corner checks (wtf uses this)

            var arch = context.Architecture;
            var wall = arch.GetWall(pos.TileX, pos.TileY, pos.Level); //todo: preprocess to check which walls are real solid walls.

            VMPlacementError wallValid = WallChangeValid(wall, direction, true);
            if (wallValid != VMPlacementError.Success) return new VMPlacementResult { Status = wallValid };

            var floor = arch.GetFloor(pos.TileX, pos.TileY, pos.Level); //todo: preprocess to check which walls are real solid walls.
            VMPlacementError floorValid = FloorChangeValid(floor, pos.Level);
            if (floorValid != VMPlacementError.Success) return new VMPlacementResult { Status = floorValid };

            //we've passed the wall test, now check if we intersect any objects.
            var valid = context.GetObjPlace(this, pos);
            return valid;
        }

        public VMPlacementError FloorChangeValid(FloorTile floor, sbyte level)
        {
            var placeFlags = (VMPlacementFlags)ObjectData[(int)VMStackObjectVariable.PlacementFlags];

            if (floor.Pattern == 65535)
            {
                if ((placeFlags & (VMPlacementFlags.AllowOnPool | VMPlacementFlags.RequirePool)) == 0) return VMPlacementError.CantPlaceOnWater;
            }
            else
            {
                if ((placeFlags & VMPlacementFlags.RequirePool) > 0) return VMPlacementError.MustPlaceOnPool;
                if (floor.Pattern == 63354)
                {
                    if ((placeFlags & (VMPlacementFlags.OnWater | VMPlacementFlags.RequireWater)) == 0) return VMPlacementError.CantPlaceOnWater;
                } else
                {
                    if ((placeFlags & VMPlacementFlags.RequireWater) > 0) return VMPlacementError.MustPlaceOnWater;
                    if (floor.Pattern == 0)
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
                        if ((placeFlags & VMPlacementFlags.OnFloor) == 0 && 
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
            if (checkUnused && ((int)placeFlags & useRotPart) > 0) return VMPlacementError.MustBeAgainstUnusedWall;

            if (((int)placeFlags & (rotPart << 8)) > 0) return VMPlacementError.CantBeThroughWall; //walls not allowed are there in this configuration
            
            return VMPlacementError.Success;
        }

        private int RotateWallSegs(WallSegments ws, int rotate) {
            int wallSides = (int)ws;
            int rotPart = ((wallSides << (4 - rotate)) & 15) | ((wallSides & 15) >> rotate);
            return rotPart;
        }

        private void SetWallUse(VMArchitecture arch, bool set)
        {
            var wall = arch.GetWall(Position.TileX, Position.TileY, Position.Level);

            var placeFlags = (WallPlacementFlags)ObjectData[(int)VMStackObjectVariable.WallPlacementFlags];
            int rotate = (8-(DirectionToWallOff(Direction) + 1)) % 4;
            byte rotPart = (byte)RotateWallSegs((WallSegments)((int)placeFlags%15), rotate);

            if (set) wall.OccupiedWalls |= (WallSegments)rotPart;
            else wall.OccupiedWalls &= (WallSegments)~rotPart;

            arch.SetWall(Position.TileX, Position.TileY, Position.Level, wall);
        }

        public void Delete(bool cleanupAll, VMContext context)
        {
            if (cleanupAll) MultitileGroup.Delete(context);
            else
            {
                PrePositionChange(context);
                context.RemoveObjectInstance(this);
                MultitileGroup.Objects.Remove(this); //we're no longer part of the multitile group
            }
        }

        public virtual void PrePositionChange(VMContext context)
        {
            if (Container != null)
            {
                Container.ClearSlot(ContainerSlot);
                return;
            }
            if (Position == LotTilePos.OUT_OF_WORLD) return;

            var arch = context.Architecture;
            if (((VMEntityFlags2)ObjectData[(int)VMStackObjectVariable.FlagField2] & (VMEntityFlags2.ArchitectualWindow | VMEntityFlags2.ArchitectualDoor)) > 0)
            { //if wall or door, attempt to place style on wall
                var placeFlags = (WallPlacementFlags)ObjectData[(int)VMStackObjectVariable.WallPlacementFlags];
                var dir = DirectionToWallOff(Direction);
                if ((placeFlags & WallPlacementFlags.WallRequiredInFront) > 0) SetWallStyle((dir) % 4, arch, 0);
                if ((placeFlags & WallPlacementFlags.WallRequiredOnRight) > 0) SetWallStyle((dir+1) % 4, arch, 0);
                if ((placeFlags & WallPlacementFlags.WallRequiredBehind) > 0) SetWallStyle((dir+2) % 4, arch, 0);
                if ((placeFlags & WallPlacementFlags.WallRequiredOnLeft) > 0) SetWallStyle((dir+3) % 4, arch, 0);
            }
            SetWallUse(arch, false);
            if (GetValue(VMStackObjectVariable.Category) == 8) context.Architecture.SetObjectSupported(Position.TileX, Position.TileY, Position.Level, false);

            if (EntryPoints[15].ActionFunction != 0)
            { //portal
                context.RemoveRoomPortal(this);
            }   
            context.UnregisterObjectPos(this);
        }

        public virtual void PositionChange(VMContext context)
        {
            if (Container != null) return;
            if (Position == LotTilePos.OUT_OF_WORLD) return;

            var arch = context.Architecture;
            if (((VMEntityFlags2)ObjectData[(int)VMStackObjectVariable.FlagField2] & (VMEntityFlags2.ArchitectualWindow | VMEntityFlags2.ArchitectualDoor)) > 0)
            { //if wall or door, attempt to place style on wall

                if (Object.OBJ.WallStyle > 21 && Object.OBJ.WallStyle < 256)
                { //first thing's first, is the style between 22-255 inclusive? If it is, then the style is stored in the object. Need to load its sprites and change the id for the objd.
                    var id = Object.OBJ.WallStyleSpriteID;
                    var style = new WallStyle()
                    {
                        WallsUpFar = Object.Resource.Get<SPR>(id),
                        WallsUpMedium = Object.Resource.Get<SPR>((ushort)(id + 1)),
                        WallsUpNear = Object.Resource.Get<SPR>((ushort)(id + 2)),
                        WallsDownFar = Object.Resource.Get<SPR>((ushort)(id + 3)),
                        WallsDownMedium = Object.Resource.Get<SPR>((ushort)(id + 4)),
                        WallsDownNear = Object.Resource.Get<SPR>((ushort)(id + 5))
                    };
                    Object.OBJ.WallStyle = TSO.Content.Content.Get().WorldWalls.AddDynamicWallStyle(style);
                }

                var placeFlags = (WallPlacementFlags)ObjectData[(int)VMStackObjectVariable.WallPlacementFlags];
                var dir = DirectionToWallOff(Direction);
                if ((placeFlags & WallPlacementFlags.WallRequiredInFront) > 0) SetWallStyle((dir) % 4, arch, Object.OBJ.WallStyle);
                if ((placeFlags & WallPlacementFlags.WallRequiredOnRight) > 0) SetWallStyle((dir+1) % 4, arch, Object.OBJ.WallStyle);
                if ((placeFlags & WallPlacementFlags.WallRequiredBehind) > 0) SetWallStyle((dir+2) % 4, arch, Object.OBJ.WallStyle);
                if ((placeFlags & WallPlacementFlags.WallRequiredOnLeft) > 0) SetWallStyle((dir+3) % 4, arch, Object.OBJ.WallStyle);
            }
            SetWallUse(arch, true);
            if (GetValue(VMStackObjectVariable.Category) == 8) context.Architecture.SetObjectSupported(Position.TileX, Position.TileY, Position.Level, true);

            if (EntryPoints[15].ActionFunction != 0)
            { //portal
                context.AddRoomPortal(this);
            }

            context.RegisterObjectPos(this);

            ExecuteEntryPoint(9, context, true); //Placement
        }

        public virtual VMPlacementError SetPosition(LotTilePos pos, Direction direction, VMContext context)
        {
            return MultitileGroup.ChangePosition(pos, direction, context);
        }

        public virtual void SetIndivPosition(LotTilePos pos, Direction direction, VMContext context, VMPlacementResult info)
        {
            Direction = direction;

            //TODO: clean the fuck up out of OUT_OF_WORLD
            if (this is VMGameObject) context.Blueprint.ChangeObjectLocation((ObjectComponent)WorldUI, (pos==LotTilePos.OUT_OF_WORLD)?LotTilePos.FromBigTile(-1,-1,1):pos);
            Position = pos;
            if (info.Container != null) info.Container.PlaceInSlot(this, 0);
        }

        private int DirectionToWallOff(Direction dir)
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

        private void SetWallStyle(int side, VMArchitecture arch, ushort value)
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

        public abstract Texture2D GetIcon(GraphicsDevice gd);
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
        ShowDynObjNameInTooltip = 1 << 14
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

    public class VMTreeByNameTableEntry
    {
        public BHAV bhav;
        public GameIffResource Owner;

        public VMTreeByNameTableEntry(BHAV bhav, GameIffResource owner)
        {
            this.bhav = bhav;
            this.Owner = owner;
        }
    }

    public class VMPieMenuInteraction
    {
        public string Name;
        public byte ID;
    }

    public struct VMSoundEntry
    {
        public HITThread Thread;
        public bool Pan;
        public bool Zoom;
        public bool Loop;
        public string Name;
    }
}
