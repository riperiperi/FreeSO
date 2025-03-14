using FSO.Content.Model;
using FSO.Files.Formats.IFF.Chunks;
using FSO.Files.Formats.IFF;
using FSO.LotView.Model;
using FSO.SimAntics.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using FSO.SimAntics.Marshals;
using FSO.SimAntics.Model.TS1Platform;
using FSO.SimAntics.Marshals.Threads;
using Microsoft.Xna.Framework;
using FSO.SimAntics.Entities;
using FSO.Vitaboy;

namespace FSO.SimAntics.Utils
{
    public class VMTS1ActivatorNew
    {
        private const int OpcodeGotoRelative = 27;
        private const int OpcodeGotoSlot = 45;
        private const int OpcodeGosub = 30;
        private const int OpcodeIdleForInput = 17;

        private class TS1MultitileBuilder
        {
            public string Name;
            public List<short> Objects;
            public List<LotTilePos> Offsets;
            public int Price;
        }

        private VM VM;
        private int Size;
        private bool FlipRoad;
        private short HouseNumber;

        public VMTS1ActivatorNew(VM vm, short hn)
        {
            this.VM = vm;
            HouseNumber = hn;
        }

        private T[] ArrayPad<T>(T[] array, int count)
        {
            if (array.Length >= count)
            {
                return array;
            }
            else
            {
                var result = new T[count];

                for (int i = 0; i < array.Length; i++)
                {
                    result[i] = array[i];
                }

                return result;
            }
        }

        private OBJD GetMasterOBJD(OBJD tile)
        {
            var file = tile.ChunkParent;
            var list = file.List<OBJD>();

            return list.FirstOrDefault((other) => other.IsMaster && other.MasterID == tile.MasterID);
        }

        private VMRoutine GetRoutine(VMStackFrameMarshal frame)
        {
            var content = Content.Content.Get();
            var res = content.WorldObjects.Get(frame.CodeOwnerGUID)?.Resource;

            if (res == null)
            {
                return null;
            }

            VMRoutine routine;
            if (frame.RoutineID >= 8192) routine = (VMRoutine)res.SemiGlobal.GetRoutine(frame.RoutineID);
            else if (frame.RoutineID >= 4096) routine = (VMRoutine)res.GetRoutine(frame.RoutineID);
            else routine = (VMRoutine)VM.Context.Globals.Resource.GetRoutine(frame.RoutineID);

            return routine;
        }

        private VMArchitectureMarshal ConvertArchitecture(IffFile iff, HOUS hous, int size)
        {
            var arch = new VMArchitectureMarshal();

            arch.Width = Size;
            arch.Height = Size;
            arch.Stories = 5;

            arch.Floors = new FloorTile[5][];
            arch.Walls = new WallTile[5][];

            // Create unused floors
            for (int i = 2; i < 5; i++)
            {
                arch.Floors[i] = new FloorTile[Size * Size];
                arch.Walls[i] = new WallTile[Size * Size];
            }

            arch.Terrain = new VMArchitectureTerrain(size, size);

            TerrainType ttype = TerrainType.GRASS;
            if (!VMTS1Activator.HouseNumToType.TryGetValue(HouseNumber, out ttype))
                ttype = TerrainType.GRASS;
            arch.Terrain.LightType = (ttype == TerrainType.SAND) ? TerrainType.GRASS : ttype;
            arch.Terrain.DarkType = ttype;

            var floorM = iff.Get<FLRm>(1)?.Entries ?? iff.Get<FLRm>(0)?.Entries ?? new List<WALmEntry>();
            var wallM = iff.Get<WALm>(1)?.Entries ?? iff.Get<WALm>(0)?.Entries ?? new List<WALmEntry>();

            var floorDict = VMTS1Activator.BuildFloorDict(floorM);
            var wallDict = VMTS1Activator.BuildWallDict(wallM);

            //altitude as 0
            var advFloors = iff.Get<ARRY>(11);
            var flags = iff.Get<ARRY>(8).TransposeData;
            if (advFloors != null)
            {
                //advanced walls and floors from modern ts1. use 16 bit wall/floor data.
                arch.Floors[0] = VMTS1Activator.RemapFloors(FlipRoad, VMTS1Activator.DecodeAdvFloors(advFloors.TransposeData), floorDict, flags);
                arch.Floors[1] = VMTS1Activator.RemapFloors(FlipRoad, VMTS1Activator.DecodeAdvFloors(iff.Get<ARRY>(111).TransposeData), floorDict, flags);
                //objects as 3
                arch.Walls[0] = VMTS1Activator.RemapWalls(VMTS1Activator.DecodeAdvWalls(iff.Get<ARRY>(12).TransposeData), wallDict, floorDict, arch.Floors[0]);
                arch.Walls[1] = VMTS1Activator.RemapWalls(VMTS1Activator.DecodeAdvWalls(iff.Get<ARRY>(112).TransposeData), wallDict, floorDict, arch.Floors[1]);
            }
            else
            {
                arch.Floors[0] = VMTS1Activator.RemapFloors(FlipRoad, VMTS1Activator.DecodeFloors(iff.Get<ARRY>(1).TransposeData), floorDict, flags);
                arch.Floors[1] = VMTS1Activator.RemapFloors(FlipRoad, VMTS1Activator.DecodeFloors(iff.Get<ARRY>(101).TransposeData), floorDict, flags);
                //objects as 3
                arch.Walls[0] = VMTS1Activator.RemapWalls(VMTS1Activator.DecodeWalls(iff.Get<ARRY>(2).TransposeData), wallDict, floorDict, arch.Floors[0]);
                arch.Walls[1] = VMTS1Activator.RemapWalls(VMTS1Activator.DecodeWalls(iff.Get<ARRY>(102).TransposeData), wallDict, floorDict, arch.Floors[1]);
            }
            //objects as 103
            arch.Terrain.GrassState = iff.Get<ARRY>(6).TransposeData.Select(x => (byte)(127 - x)).ToArray();

            //targetgrass is 7
            //flags is 8/108
            var pools = iff.Get<ARRY>(9).TransposeData;
            var water = iff.Get<ARRY>(10).TransposeData;

            for (int i = 0; i < pools.Length; i++)
            {
                //pools in freeso are slightly different
                if (pools[i] != 0xff && pools[i] != 0x0) arch.Floors[0][i].Pattern = 65535;
                if (water[i] != 0xff && water[i] != 0x0) arch.Floors[0][i].Pattern = 65534;
            }

            arch.Floors[0] = VMTS1Activator.ResizeFloors(arch.Floors[0], size);
            arch.Floors[1] = VMTS1Activator.ResizeFloors(arch.Floors[1], size);
            arch.Walls[0] = VMTS1Activator.ResizeWalls(arch.Walls[0], size);
            arch.Walls[1] = VMTS1Activator.ResizeWalls(arch.Walls[1], size);
            arch.FineBuildableArea = VMTS1Activator.ResizeFlags(flags, size);
            arch.Terrain.GrassState = VMTS1Activator.ResizeGrass(arch.Terrain.GrassState, size);
            arch.Terrain.Heights = Array.ConvertAll(VMTS1Activator.ResizeGrass(VMTS1Activator.DecodeHeights(iff.Get<ARRY>(0).TransposeData), size), x => (short)(x * 10));
            arch.Terrain.RegenerateCenters();
            arch.RoofStyle = (uint)Content.Content.Get().WorldRoofs.NameToID(hous.RoofName.ToLowerInvariant() + ".bmp");

            arch.FloorsDirty = true;
            arch.WallsDirty = true;

            return arch;
        }

        private TTABFlags ConvertFlags(OBJMInteractionFlags flags)
        {
            TTABFlags result = 0;

            if ((flags & OBJMInteractionFlags.PushHeadContinuation) != 0)
            {
                result |= TTABFlags.FSOPushHead;
            }

            return result;
        }

        private VMQueuedActionMarshal ConvertAction(OBJM objm, OBJT objt, OBJMInstance inst, OBJMInteraction action)
        {
            var calleeObj = objm.ObjectData[action.TargetID].Instance;

            return new VMQueuedActionMarshal()
            {
                UID = (ushort)action.UID,
                RoutineID = (ushort)action.ActionTreeID,
                CheckRoutineID = 0, // In TS1, it seems to find this each check with the TTA index.
                Callee = action.TargetID,
                StackObject = action.TargetID,
                CodeOwnerGUID = calleeObj.OBJT.GUID,
                IconOwner = action.Icon,
                Args = action.Args,
                Priority = (short)action.Priority,
                Name = "", // TODO: recover this

                Mode = Engine.VMQueueMode.Normal,
                // TODO: TS1 doesn't save a lot of flags for checks, so we need to make the game fetch it when necessary.
                // Not up for fetching the TTAB from here, so skip permission checks when loading for now.
                Flags = TTABFlags.FSOSkipPermissions | ConvertFlags(action.Flags),
                Flags2 = 0, // This isn't TSO.
                NotifyIdle = action.Flags.HasFlag(OBJMInteractionFlags.UserInterrupted), // Also impacted by priority, but this one is obvious enough.

                InteractionNumber = action.TTAIndex,
            };
        }

        private VMThreadMarshal ConvertThread(OBJM objm, OBJT objt, OBJMInstance inst)
        {
            var thread = new VMThreadMarshal();
            short myID = inst.ObjectData[(int)VMStackObjectVariable.ObjectId];

            thread.Stack = new VMStackFrameMarshal[inst.Stack.Length];
            thread.TempRegisters = ArrayPad(inst.TempRegisters, 20); // FSO has 20 temp registers rather than 8

            for (int i = 0; i < thread.Stack.Length; i++)
            {
                var frame = inst.Stack[i];

                var ownerType = objt.Entries[frame.CodeOwnerObjType - 1];

                thread.Stack[i] = new VMStackFrameMarshal()
                {
                    StackObject = frame.StackObjectID,
                    RoutineID = (ushort)frame.TreeID,
                    CodeOwnerGUID = ownerType.GUID,
                    InstructionPointer = (ushort)frame.NodeID,

                    // FreeSO expects at least 4 arguments in a bunch of places - TS1 is more strict.
                    Args = ArrayPad(frame.Parameters, 4),
                    Locals = frame.Locals,

                    Caller = myID,
                    Callee = myID, // FreeSO specfic thing that probably should have been removed. Sort of restored from use counts for people.

                    ActionTree = false, // This is restored later for person type objects.
                    SpecialResult = Engine.VMSpecialResult.Normal,
                };
            }

            if (inst.PersonData != null)
            {
                // Restore interaction queue, use counts
                var person = inst.PersonData.Value;

                int activeCount = person.ActiveInteraction.IsValid() ? 1 : 0;
                thread.ActiveQueueBlock = (sbyte)(activeCount - 1);
                thread.Queue = new VMQueuedActionMarshal[activeCount + person.InteractionQueue.Length];

                short activePriority = person.PersonData[(int)VMPersonDataVariable.Priority];
                if (activeCount != 0)
                {
                    thread.Queue[0] = ConvertAction(objm, objt, inst, person.ActiveInteraction);
                    thread.ActionUID = thread.Queue[0].UID;
                }

                for (int j = 0; j < person.InteractionQueue.Length; j++)
                {
                    thread.Queue[activeCount + j] = ConvertAction(objm, objt, inst, person.InteractionQueue[j]);

                    if (activeCount != 0 && thread.Queue[activeCount + j].Priority > activePriority)
                    {
                        thread.Queue[0].NotifyIdle = true;
                    }
                }

                thread.Interrupt = false; // TODO: Don't really know a good mapping for this...

                // Try to restore callees for the stack and some special state.

                foreach (var use in person.ObjectUses)
                {
                    var frame = thread.Stack[use.StackLength - 1];
                    var inAction = false;

                    if (use.StackLength > 1)
                    {
                        // This is a bit of an adapter. When returning from a stack frame, the parent frame needs to handle the result.
                        // For subroutines, the result of the frame just determines where we go from the subroutine node.
                        // Calling named trees works the same way.
                        // In TS1, certain types of primitive handle this differently. Gosub found action and idle for input will ignore results from interactions.
                        // They also remove the interaction from being active, obviously.
                        // In FreeSO, this is implemented as a SpecialResult flag on the stack frame, but it _should_ be tied to the primitive.
                        // Routing primitives are another one that have special handling for returns from portal functions.
                        // Normally, FreeSO handles these because routing actually occupies space in the stack, and the result code goes into the routing frame.
                        // When restoring TS1 stacks though, it just goes straight back into the calling tree with the routing primitive...
                        // It should rerun in this case, since we don't have any routing state in the save.
                        var callingFrame = thread.Stack[use.StackLength - 2];

                        var routine = GetRoutine(callingFrame);
                        var prim = routine.Instructions[callingFrame.InstructionPointer];

                        if (prim.Opcode == OpcodeGotoRelative || prim.Opcode == OpcodeGotoSlot)
                        {
                            frame.SpecialResult = Engine.VMSpecialResult.Retry;
                        }
                        else if (prim.Opcode == OpcodeGosub || prim.Opcode == OpcodeIdleForInput)
                        {
                            frame.SpecialResult = Engine.VMSpecialResult.Interaction;
                            inAction = true;
                        }
                    }

                    for (int i = use.StackLength - 1; i < thread.Stack.Length; i++)
                    {
                        thread.Stack[i].Callee = use.TargetID;
                        thread.Stack[i].ActionTree |= inAction;
                    }
                }
            }
            else
            {
                thread.ActiveQueueBlock = -1;
                thread.Queue = new VMQueuedActionMarshal[0];
                thread.Interrupt = false;
                thread.ActionUID = 0;
            }

            return thread;
        }

        private VMAnimationStateMarshal ConvertAnimation(string anim, int eventsRun)
        {
            var split = anim.Split(';');

            //name;priority;speed (1/1000ths);frame;weight? (1/1000ths);loop;unk;unk

            var name = split[0];
            var speed = int.Parse(split[2]);
            var frame = int.Parse(split[3]);
            var weight = int.Parse(split[4]);
            var loop = split[5] == "1";

            var anims = Content.Content.Get().AvatarAnimations;

            var res = anims.Get($"{name}.anim");
            int frames = res == null ? 1 : res.NumFrames;

            int end = speed < 0 ? 0 : 1000;

            return new VMAnimationStateMarshal()
            {
                Anim = name,
                Speed = Math.Abs(speed) / 1000f,
                CurrentFrame = frames * (frame / 1000f),
                Weight = weight / 1000f,
                Loop = loop,
                EndReached = frame == end,
                PlayingBackwards = speed < 0,
                // TODO: The original seems to use events count determine this live,
                // rather than the animation queuing events like in FreeSO.
                // This might skip events right now...
                EventQueue = new short[0], 
                EventsRun = (byte)eventsRun,
            };
        }

        private VMAvatarMarshal ConvertAvatar(OBJMInstance inst)
        {
            var person = inst.PersonData.Value;

            var bodyStrings = inst.OBJD.ChunkParent.Get<STR>(inst.OBJD.BodyStringID);

            var ava = new VMAvatarMarshal()
            {
                Animations = person.Animation == "" ? new VMAnimationStateMarshal[0] : new VMAnimationStateMarshal[]
                {
                    ConvertAnimation(person.Animation, person.AnimEventCount)
                },
                CarryAnimationState = person.CarryAnimation == "" ? null : ConvertAnimation(person.CarryAnimation, 0),

                MotiveChanges = new VMMotiveChange[16],
                MotiveDecay = new VMTS1MotiveDecay(),
                PersonData = person.PersonData,
                MotiveData = person.MotiveData.Select(motive =>
                {
                    return (short)Math.Round(motive);
                }).ToArray(),

                RadianDirection = inst.ObjectData[(int)VMStackObjectVariable.Direction] * (float)(Math.PI / 4.0),

                DefaultSuits = new VMAvatarDefaultSuits(false)
                {
                    Daywear = new VMOutfitReference(bodyStrings, false)
                },
                DynamicSuits = new VMAvatarDynamicSuits(false), // Not used in TS1
                Decoration = new VMAvatarDecoration(),
                BoundAppearances = person.Accessories.Select(accessory => accessory.Name).ToArray(),

                BodyOutfit = person.Body == "" ? null : new VMOutfitReference($"{person.Body},{person.BodyTex}", false),
                HeadOutfit = person.Head == "" ? null : new VMOutfitReference($"{person.Head},{person.HeadTex}", true),
                SkinTone = (AppearanceType)person.PersonData[(int)VMPersonDataVariable.SkinColor]
            };

            for (int i = 0; i < 16; i++)
            {
                ava.MotiveChanges[i] = new VMMotiveChange();
                ava.MotiveChanges[i].Motive = (VMMotive)i;
            }

            foreach (var delta in person.MotiveDeltas)
            {
                var target = ava.MotiveChanges[delta.Motive];
                target.MaxValue = (short)delta.StopAt;
                target.PerHourChange = (short)Math.Round(delta.TickDelta * 1800);
            }

            return ava;
        }

        public Blueprint LoadFromIff(IffFile iff)
        {
            var content = Content.Content.Get();
            var simi = iff.Get<SIMI>(1);
            var hous = iff.Get<HOUS>(0);

            var neighbors = content.Neighborhood.Neighbors;

            var fsov = new VMMarshal();

            fsov.TS1 = true;
            fsov.GlobalState = simi.GlobalData.ToArray();

            Size = simi.GlobalData[23];
            var type = simi.GlobalData[35];

            fsov.GlobalState[20] = 255; //Game Edition. Basically, what "expansion packs" are running. Let's just say all of them.
            fsov.GlobalState[25] = 4; //as seen in EA-Land edith's simulator globals, this needs to be set for people to do their idle interactions.
            fsov.GlobalState[17] = 4; //Runtime Code Version, is this in EA-Land.

            var selectedPerson = fsov.GlobalState[3];

            //VM.SetGlobalValue(3, 0); //Selected Sim ID. Default to 0.
            //VM.SetGlobalValue(9, 0); //Active Family ID. Default to 0.

            //VM.SetGlobalValue(10, HouseNumber); //set house number
            //VM.SetGlobalValue(32, 0); //simless build mode
            //VM.SetGlobalValue(33, 2); //machine level

            // Init architecture and other context stuff

            FlipRoad = (hous.CameraDir & 1) > 0;

            var arch = ConvertArchitecture(iff, hous, Size);

            var clock = new VMClockMarshal
            {
                TicksPerMinute = 30,
                Hours = fsov.GlobalState[0],
                DayOfMonth = fsov.GlobalState[1],
                Minutes = fsov.GlobalState[5],
                Month = fsov.GlobalState[7],
                Year = fsov.GlobalState[8]
            };
            clock.MinuteFractions = fsov.GlobalState[6] * clock.TicksPerMinute;

            var context = new VMContextMarshal
            {
                Architecture = arch,
                Clock = clock,
                Ambience = new VMAmbientSoundMarshal(),
                RandomSeed = (ulong)((new Random()).NextDouble() * ulong.MaxValue)
            };

            fsov.Context = context;

            var sims1 = new VMTS1LotState();
            sims1.SimulationInfo = simi;
            // vm is initialized at the end...

            fsov.PlatformState = sims1;

            // Load objects

            var objt = iff.Get<OBJT>(0);
            var objm = iff.Get<OBJM>(1);

            objm.Prepare((ushort typeID) =>
            {
                var entry = objt.Entries[typeID - 1];
                return new OBJMResource()
                {
                    OBJD = content.WorldObjects.Get(entry.GUID)?.OBJ,
                    OBJT = entry
                };
            });

            var objectCount = objm.ObjectData.Count;

            var objects = new List<VMEntityMarshal>();
            var threads = new List<VMThreadMarshal>();
            var groups = new List<VMMultitileGroupMarshal>();
            var groupBuilders = new Dictionary<short, TS1MultitileBuilder>();

            var objsById = objm.ObjectData.Values.OrderBy(obj => obj.Instance.ObjectData[(int)VMStackObjectVariable.ObjectId]);

            foreach (var obj in objsById)
            {
                var inst = obj.Instance;

                if (inst.OBJD == null)
                {
                    if (inst.PersonData.HasValue)
                    {
                        var person = inst.PersonData.Value;
                        var neighborId = person.PersonData[(int)VMPersonDataVariable.NeighborId];

                        if (neighbors.NeighbourByID.TryGetValue(neighborId, out Neighbour neighbor) && inst.OBJT.Name == neighbor.Name)
                        {
                            // Last chance recovery - doesn't tend to succeed.
                            // The unleashed premade families tend to have the wrong GUID doe to having their save copied from another hood.
                            inst.OBJT.GUID = neighbor.GUID;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        continue;
                    }
                }

                var master = inst.MultitileData.HasValue ? GetMasterOBJD(inst.OBJD) : null;

                VMEntityMarshal ent;

                if (inst.PersonData != null)
                {
                    var ava = ConvertAvatar(inst);

                    ava.PlatformState = new VMTS1AvatarState();

                    ent = ava;
                }
                else
                {
                    var gobj = new VMGameObjectMarshal();

                    gobj.Direction = (Direction)(1 << inst.ObjectData[(int)VMStackObjectVariable.Direction]);
                    gobj.PlatformState = new VMTS1ObjectState();

                    ent = gobj;
                }

                ent.TS1 = true;
                ent.GUID = inst.OBJT.GUID;
                ent.MasterGUID = master == null ? 0 : master.GUID;

                ent.Position = inst.X == -16 && inst.Y == -16 ?
                    LotTilePos.OUT_OF_WORLD :
                    new LotTilePos((short)inst.X, (short)inst.Y, (sbyte)inst.Level);

                ent.Attributes = inst.Attributes;
                ent.MyList = new short[0];
                ent.ObjectData = inst.ObjectData;
                ent.ObjectID = inst.ObjectData[(int)VMStackObjectVariable.ObjectId];
                ent.PersistID = selectedPerson == ent.ObjectID ? 65537 : (uint)ent.ObjectID;
                ent.Container = inst.ObjectData[(int)VMStackObjectVariable.ContainerId];
                ent.ContainerSlot = inst.ObjectData[(int)VMStackObjectVariable.SlotNumber];

                ent.DynamicSpriteFlags = 0;
                ent.DynamicSpriteFlags2 = 0;

                for (int i = 0; i < inst.DynamicSpriteFlags.Length; i++)
                {
                    if (inst.DynamicSpriteFlags[i] != 0)
                    {
                        if (i < 64)
                        {
                            ent.DynamicSpriteFlags |= 1ul << i;
                        }
                        else
                        {
                            // TODO: some objects have more than 128 flags
                            ent.DynamicSpriteFlags2 |= 1ul << (i - 64);
                        }
                    }
                }

                ent.LightColor = Color.White;

                ent.Contained = inst.Slots.Select(x => x.ObjectID).ToArray();

                // Relationships

                ent.MeToObject = inst.Relationships.Select((rel) =>
                {
                    return new VMEntityRelationshipMarshal()
                    {
                        Target = (ushort)rel.TargetID,
                        Values = rel.Values.Select(x => (short)x).ToArray()
                    };
                }).ToArray();

                // FSO persist relationships are not used in TS1.
                ent.MeToPersist = new VMEntityPersistRelationshipMarshal[0];

                objects.Add(ent);
                threads.Add(ConvertThread(objm, objt, inst));

                if (inst.MultitileData == null)
                {
                    groups.Add(new VMMultitileGroupMarshal()
                    {
                        MultiTile = false,
                        Name = inst.OBJT.Name,
                        Objects = new short[] { ent.ObjectID },
                        Offsets = new LotTilePos[] { new LotTilePos() },
                        Price = inst.OBJD.Price,
                        SalePrice = -1,
                    });
                }
                else
                {
                    var mt = inst.MultitileData.Value;
                    short lead = mt.MultitileParentID == 0 ? ent.ObjectID : mt.MultitileParentID;
                    if (!groupBuilders.TryGetValue(lead, out TS1MultitileBuilder builder))
                    {
                        builder = new TS1MultitileBuilder()
                        {
                            Name = master.ChunkLabel,
                            Objects = new List<short>(),
                            Offsets = new List<LotTilePos>(),
                            Price = master.Price,
                        };

                        groupBuilders.Add(lead, builder);
                    }

                    var offset = LotTilePos.FromBigTile((short)mt.GroupX, (short)mt.GroupY, (sbyte)mt.GroupLevel);

                    if (lead == ent.ObjectID)
                    {
                        builder.Objects.Insert(0, ent.ObjectID);
                        builder.Offsets.Insert(0, offset);
                    }
                    else
                    {
                        builder.Objects.Add(ent.ObjectID);
                        builder.Offsets.Add(offset);
                    }
                }
            }

            foreach (var builder in groupBuilders.Values)
            {
                groups.Add(new VMMultitileGroupMarshal()
                {
                    MultiTile = true,
                    Name = builder.Name,
                    Objects = builder.Objects.ToArray(),
                    Offsets = builder.Offsets.ToArray(),
                    Price = builder.Price,
                    SalePrice = -1,
                });
            }

            fsov.Entities = objects.ToArray();
            fsov.Threads = threads.ToArray();
            fsov.MultitileGroups = groups.ToArray();

            VM.Load(fsov);
            VM.UpdateFreeObjectID();

            // Attempt to recover queue names.
            foreach (var ava in VM.Context.ObjectQueries.Avatars)
            {
                foreach (var action in ava.Thread.Queue)
                {
                    var ttas = action.Callee.TreeTableStrings;
                    //var ttab = action.Callee.TreeTable;

                    if (ttas != null)
                    {
                        action.Name = ttas.GetString(action.InteractionNumber);

                        // TODO: rerun check tree and match param 0 to restore changed action names if possible
                    }
                }
            }

            return VM.Context.Blueprint;
        }
    }
}
