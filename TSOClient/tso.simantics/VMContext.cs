using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using tso.world;
using TSO.Simantics.engine;
using TSO.Simantics.engine.primitives;
using TSO.Simantics.primitives;
using TSO.Content;
using TSO.Files.formats.iff;
using tso.world.model;
using tso.world.components;
using TSO.Files.formats.iff.chunks;
using Microsoft.Xna.Framework;
using TSO.Simantics.model;
using TSO.Simantics.entities;

namespace TSO.Simantics
{
    public class VMContext
    {
        public Blueprint Blueprint;
        public VMClock Clock { get; internal set; }
        public World World { get; internal set; }
        public Dictionary<ushort, VMPrimitiveRegistration> Primitives = new Dictionary<ushort, VMPrimitiveRegistration>();
        public VMAmbientSound Ambience;
        private ulong RandomSeed;

        public GameGlobal Globals;
        public VMRoomInfo[] RoomInfo;
        private Dictionary<int, List<short>> ObjectsAt; //used heavily for routing
        
        public VM VM;

        public VMContext(World world){
            this.World = world;
            this.Clock = new VMClock();
            this.Ambience = new VMAmbientSound();

            ObjectsAt = new Dictionary<int, List<short>>();

            RandomSeed = (ulong)((new Random()).NextDouble() * UInt64.MaxValue); //when resuming state, this should be set.
            Clock.TicksPerMinute = 30; //1 minute per irl second

            AddPrimitive(new VMPrimitiveRegistration(new VMGenericTSOCall())
            {
                Opcode = 1,
                Name = "generic_sims_online_call",
                OperandModel = typeof(VMGenericTSOCallOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMGotoRoutingSlot()) {
                Opcode = 45,
                Name = "goto_routing_slot",
                OperandModel = typeof(VMGotoRoutingSlotOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMAnimateSim()) {
                Opcode = 44,
                Name = "animate",
                OperandModel = typeof(VMAnimateSimOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMRelationship())
            {
                Opcode = 24,
                Name = "old_relationship",
                OperandModel = typeof(VMOldRelationshipOperand) //same primitive, different operand
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMRelationship())
            {
                Opcode = 26,
                Name = "relationship",
                OperandModel = typeof(VMRelationshipOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMFindLocationFor())
            {
                Opcode = 16,
                Name = "find_location_for",
                OperandModel = typeof(VMFindLocationForOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMTestSimInteractingWith()) {
                Opcode = 37,
                Name = "test_sim_interacting_with",
                OperandModel = typeof(VMTestSimInteractingWithOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMFindBestAction())
            {
                Opcode = 65,
                Name = "find_best_action",
                OperandModel = typeof(VMFindBestActionOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMGrab())
            {
                Opcode = 4,
                Name = "grab",
                OperandModel = typeof(VMGrabOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMDropOnto())
            {
                Opcode = 43,
                Name = "drop_onto",
                OperandModel = typeof(VMDropOntoOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMReach())
            {
                Opcode = 47,
                Name = "reach",
                OperandModel = typeof(VMReachOperand)
            });


            AddPrimitive(new VMPrimitiveRegistration(new VMLookTowards())
            {
                Opcode = 22,
                Name = "look_towards",
                OperandModel = typeof(VMLookTowardsOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMSysLog())
            {
                Opcode = 30,
                Name = "syslog",
                OperandModel = typeof(VMSysLogOperand)
            });


            AddPrimitive(new VMPrimitiveRegistration(new VMGetDistanceTo())
            {
                Opcode = 11,
                Name = "get_distance_to",
                OperandModel = typeof(VMGetDistanceToOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMGetDirectionTo())
            {
                Opcode = 12,
                Name = "get_direction_to",
                OperandModel = typeof(VMGetDirectionToOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMBreakPoint())
            {
                Opcode = 15,
                Name = "breakpoint",
                OperandModel = typeof(VMBreakPointOperand)
            });


            AddPrimitive(new VMPrimitiveRegistration(new VMChangeActionString())
            {
                Opcode = 50,
                Name = "change_action_string",
                OperandModel = typeof(VMChangeActionStringOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMSnap()) //not functional right now
            {
                Opcode = 46,
                Name = "snap",
                OperandModel = typeof(VMSnapOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMCreateObjectInstance())
            {
                Opcode = 42,
                Name = "create_object_instance",
                OperandModel = typeof(VMCreateObjectInstanceOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMRemoveObjectInstance())
            {
                Opcode = 18,
                Name = "remove_object_instance",
                OperandModel = typeof(VMRemoveObjectInstanceOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMIdleForInput())
            {
                Opcode = 17,
                Name = "idle_for_input",
                OperandModel = typeof(VMIdleForInputOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMRunTreeByName())
            {
                Opcode = 28,
                Name = "run_tree_by_name",
                OperandModel = typeof(VMRunTreeByNameOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMPushInteraction())
            {
                Opcode = 13,
                Name = "push_interaction",
                OperandModel = typeof(VMPushInteractionOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMFindBestObjectForFunction())
            {
                Opcode = 14,
                Name = "find_best_object_for_function",
                OperandModel = typeof(VMFindBestObjectForFunctionOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMRunFunctionalTree())
            {
                Opcode = 20,
                Name = "run_functional_tree",
                OperandModel = typeof(VMRunFunctionalTreeOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration (new VMExpression()) {
                Opcode = 2,
                Name = "expression",
                OperandModel = typeof(VMExpressionOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMRandomNumber())
            {
                Opcode = 8,
                Name = "random_number",
                OperandModel = typeof(VMRandomNumberOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMPlaySound())
            {
                Opcode = 23,
                Name = "play_sound",
                OperandModel = typeof(VMPlaySoundOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMSleep())
            {
                Opcode = 0,
                Name = "sleep",
                OperandModel = typeof(VMSleepOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMRefresh())
            {
                Opcode = 7,
                Name = "refresh",
                OperandModel = typeof(VMRefreshOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMNotifyOutOfIdle())
            {
                Opcode = 49,
                Name = "stackobj_notify_out_of_idle",
                OperandModel = typeof(VMAnimateSimOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMSetMotiveChange())
            {
                Opcode = 29,
                Name = "set_motive_deltas",
                OperandModel = typeof(VMSetMotiveChangeOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMChangeSuitOrAccessory())
            {
                Opcode = 6,
                Name = "change_suit_or_accessory",
                OperandModel = typeof(VMChangeSuitOrAccessoryOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMTransferFunds())
            {
                Opcode = 25,
                Name = "transfer_funds",
                OperandModel = typeof(VMTransferFundsOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMSetToNext())
            {
                Opcode = 31,
                Name = "set_to_next",
                OperandModel = typeof(VMSetToNextOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMGotoRelativePosition())
            {
                Opcode = 27,
                Name = "goto_relative",
                OperandModel = typeof(VMGotoRelativePositionOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMTestObjectType())
            {
                Opcode = 32,
                Name = "test_object_type",
                OperandModel = typeof(VMTestObjectTypeOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMSpecialEffect())
            {
                Opcode = 35,
                Name = "special_effect",
                OperandModel = typeof(VMSpecialEffectOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMSetBalloonHeadline())
            {
                Opcode = 41,
                Name = "set_balloon_headline",
                OperandModel = typeof(VMSetBalloonHeadlineOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMDialogPrivateStrings())
            {
                Opcode = 36,
                Name = "dialog_private",
                OperandModel = typeof(VMDialogStringsOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMDialogSemiGlobalStrings())
            {
                Opcode = 39,
                Name = "dialog_semiglobal",
                OperandModel = typeof(VMDialogStringsOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMDialogGlobalStrings())
            {
                Opcode = 38,
                Name = "dialog_global",
                OperandModel = typeof(VMDialogStringsOperand)
            });
            AddPrimitive(new VMPrimitiveRegistration(new VMStopAllSounds())
            {
                Opcode = 48,
                Name = "stop_all_sounds",
                OperandModel = typeof(VMStopAllSoundsOperand)
            });
        }

        /// <summary>
        /// Returns a random number between 0 and less than the specified maximum.
        /// </summary>
        /// <param name="max">The upper bound of the random number.</param>
        /// <returns></returns>
        public ulong NextRandom(ulong max)
        {
            if (max == 0) return 0;
            RandomSeed = (RandomSeed * 274876858367) + 1046527;
            return RandomSeed % max;
        }

        public void RegeneratePortalInfo()
        {
            RoomInfo = new VMRoomInfo[Blueprint.RoomData.Count()];
            for (int i = 0; i < RoomInfo.Length; i++)
            {
                RoomInfo[i].Portals = new List<VMRoomPortal>();
            }

            foreach (var obj in VM.Entities)
            {
                if (obj.EntryPoints[15].ActionFunction != 0)
                { //portal object
                    AddRoomPortal(obj);
                }
            }
        }

        public void AddRoomPortal(VMEntity obj)
        {
            var room = GetObjectRoom(obj);

            //find other portal part, must be in other room to count...
            foreach (var obj2 in obj.MultitileGroup.Objects)
            {
                var room2 = GetObjectRoom(obj2);
                if (obj != obj2 && room2 != room && obj2.EntryPoints[15].ActionFunction != 0)
                {
                    RoomInfo[room].Portals.Add(new VMRoomPortal(obj.ObjectID, room2));
                    break;
                }
            }
        }

        public void RemoveRoomPortal(VMEntity obj)
        {
            var room = GetObjectRoom(obj);
            VMRoomPortal target = null;
            foreach (var port in RoomInfo[room].Portals)
            {
                if (port.ObjectID == obj.ObjectID)
                {
                    target = port;
                    break;
                }
            }
            if (target != null) RoomInfo[room].Portals.Remove(target);
        }

        public void RegisterObjectPos(VMEntity obj)
        {
            var pos = obj.Position;
            if (!ObjectsAt.ContainsKey(pos.TileID)) ObjectsAt[pos.TileID] = new List<short>();
            ObjectsAt[pos.TileID].Add(obj.ObjectID);
        }

        public void UnregisterObjectPos(VMEntity obj)
        {
            var pos = obj.Position;
            if (ObjectsAt.ContainsKey(pos.TileID)) ObjectsAt[pos.TileID].Remove(obj.ObjectID);
        }

        public VMSolidResult SolidToAvatars(LotTilePos pos)
        {
            if (!ObjectsAt.ContainsKey(pos.TileID)) return new VMSolidResult();
            var objs = ObjectsAt[pos.TileID];
            foreach (var id in objs)
            {
                var obj = VM.GetObjectById(id);
                if (obj == null) continue;
                var flags = (VMEntityFlags)obj.GetValue(VMStackObjectVariable.Flags);
                if (((flags & VMEntityFlags.DisallowPersonIntersection) > 0) || (flags & (VMEntityFlags.AllowPersonIntersection | VMEntityFlags.HasZeroExtent)) == 0) 
                    return new VMSolidResult { 
                        Solid = true,
                        Chair = (obj.EntryPoints[26].ActionFunction != 0)?obj:null
                    }; //solid to people
            }
            return new VMSolidResult();
        }

        public bool IsOutOfBounds(LotTilePos pos)
        {
            return (pos.x < 0 || pos.y < 0 || pos.TileX >= Blueprint.Width || pos.TileY >= Blueprint.Height);
        }

        public VMPlacementResult GetObjPlace(VMEntity target, LotTilePos pos)
        {
            //ok, this might be confusing...
            short allowedHeights = target.GetValue(VMStackObjectVariable.AllowedHeightFlags);
            short weight = target.GetValue(VMStackObjectVariable.Weight);
            var tflags = (VMEntityFlags)target.GetValue(VMStackObjectVariable.Flags);
            bool noFloor = (allowedHeights&1)==0;

            VMPlacementError status = (noFloor)?VMPlacementError.HeightNotAllowed:VMPlacementError.Success;

            if ((tflags & VMEntityFlags.HasZeroExtent) > 0 || (!ObjectsAt.ContainsKey(pos.TileID))) return new VMPlacementResult { Status = status };
            var objs = ObjectsAt[pos.TileID];
            foreach (var id in objs)
            {
                var obj = VM.GetObjectById(id);
                if (obj == null || obj.MultitileGroup == target.MultitileGroup) continue;
                var flags = (VMEntityFlags)obj.GetValue(VMStackObjectVariable.Flags);
                if ((flags & VMEntityFlags.HasZeroExtent) == 0)
                {
                    status = VMPlacementError.CantIntersectOtherObjects;
                    
                    //this object is technically solid. Check if we can place on top of it
                    if (allowedHeights>1 && obj.TotalSlots() > 0 && obj.GetSlot(0) == null)
                    {
                        //first check if we have a slot 0, which is what we place onto. then check if it's empty, 
                        //then check if the object can support this one's weight.
                        //we also need to make sure that the height of this specific slot is allowed.

                        if (((1 << (obj.GetSlotHeight(0) - 1)) & allowedHeights) > 0)
                        {
                            if (weight < obj.GetValue(VMStackObjectVariable.SupportStrength))
                            {
                                return new VMPlacementResult
                                {
                                    Status = VMPlacementError.Success,
                                    Container = obj
                                };
                            }
                            else
                            {
                                status = VMPlacementError.CantSupportWeight;
                            }
                        }
                        else
                        {
                            if (noFloor)
                            {
                                if ((allowedHeights & (1 << 3)) > 0) status = VMPlacementError.CounterHeight;
                                else status = (obj.GetSlotHeight(0) == 8) ? VMPlacementError.CannotPlaceComputerOnEndTable : VMPlacementError.HeightNotAllowed;
                            }
                        }
                    }
                }
            }
            return new VMPlacementResult
            {
                Status = status
            };
        }

        public ushort GetObjectRoom(VMEntity obj)
        {
            return Blueprint.Rooms.Map[obj.Position.TileX + obj.Position.TileY*Blueprint.Width];
        }

        public ushort GetRoomAt(LotTilePos pos)
        {
            if (pos.TileX < 0 || pos.TileX > Blueprint.Width) return 0;
            else if (pos.TileY < 0 || pos.TileY > Blueprint.Height) return 0;
            else return Blueprint.Rooms.Map[pos.TileX + pos.TileY * Blueprint.Width];
        }

        public VMMultitileGroup CreateObjectInstance(UInt32 GUID, LotTilePos pos, Direction direction)
        {

            VMMultitileGroup group = new VMMultitileGroup();
            var objDefinition = TSO.Content.Content.Get().WorldObjects.Get(GUID);
            if (objDefinition == null)
            {
                return null;
            }

            var master = objDefinition.OBJ.MasterID;
            if (master != 0)
            {
                group.MultiTile = true;
                var objd = objDefinition.Resource.List<OBJD>();

                for (int i = 0; i < objd.Count; i++)
                {
                    if (objd[i].MasterID == master && objd[i].SubIndex != -1) //if sub-part of this object, make it!
                    {
                        var subObjDefinition = TSO.Content.Content.Get().WorldObjects.Get(objd[i].GUID);
                        if (subObjDefinition != null)
                        {
                            var worldObject = new ObjectComponent(subObjDefinition);
                            var vmObject = new VMGameObject(subObjDefinition, worldObject);
                            vmObject.MasterDefinition = objDefinition.OBJ;
                            vmObject.UseTreeTableOf(objDefinition);
                            group.Objects.Add(vmObject);

                            vmObject.MultitileGroup = group;
                            VM.AddEntity(vmObject);
                            
                        }
                    }
                }

                group.Init(this);
                group.ChangePosition(pos, direction, this);
                return group;
            }
            else
            {
                if (objDefinition.OBJ.ObjectType == OBJDType.Person) //person
                {
                    var vmObject = new VMAvatar(objDefinition);
                    vmObject.MultitileGroup = group;
                    group.Objects.Add(vmObject);
                    VM.AddEntity(vmObject);

                    Blueprint.AddAvatar((AvatarComponent)vmObject.WorldUI);

                    group.Init(this);
                    vmObject.SetPosition(pos, direction, this);
                 
                    return group;
                }
                else
                {
                    var worldObject = new ObjectComponent(objDefinition);
                    var vmObject = new VMGameObject(objDefinition, worldObject);

                    vmObject.MultitileGroup = group;
                    group.Objects.Add(vmObject);

                    VM.AddEntity(vmObject);

                    group.Init(this);
                    vmObject.SetPosition(pos, direction, this);
                    
                    return group;
                }
            }
        }

        public void RemoveObjectInstance(VMEntity target)
        {
            target.PrePositionChange(this);
            VM.RemoveEntity(target);
            if (target is VMGameObject) Blueprint.RemoveObject((ObjectComponent)target.WorldUI);
            else Blueprint.RemoveAvatar((AvatarComponent)target.WorldUI);
        }

        public VMPrimitiveRegistration GetPrimitive(ushort opcode)
        {
            if (Primitives.ContainsKey(opcode)){
                return Primitives[opcode];
            }
            return null;
        }

        public void AddPrimitive(VMPrimitiveRegistration primitive){
            Primitives.Add(primitive.Opcode, primitive);
        }

        public void ThreadIdle(VMThread thread){
            /** Switch thread to idle **/
            VM.ThreadIdle(thread);
        }

        public void ThreadActive(VMThread thread){
            /** Switch thread to active **/
            VM.ThreadActive(thread);
        }

        public void ThreadRemove(VMThread thread)
        {
            /** Stop updating a thread **/
            VM.ThreadRemove(thread);
        }
    }

    public struct VMSolidResult
    {
        public bool Solid;
        public VMEntity Chair;
    }

    public struct VMPlacementResult
    {
        public VMPlacementError Status; //if true, cannot place anywhere.
        public VMEntity Container; //NULL if on floor
    }
}
