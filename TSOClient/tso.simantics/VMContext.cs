/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using FSO.LotView;
using FSO.SimAntics.Engine;
using FSO.SimAntics.Engine.Primitives;
using FSO.SimAntics.Primitives;
using FSO.Content;
using FSO.Files.Formats.IFF;
using FSO.LotView.Model;
using FSO.LotView.Components;
using FSO.Files.Formats.IFF.Chunks;
using Microsoft.Xna.Framework;
using FSO.SimAntics.Model;
using FSO.SimAntics.Entities;
using FSO.SimAntics.Model.Routing;

namespace FSO.SimAntics
{
    public class VMContext
    {
        public static bool UseWorld = true;
        public Blueprint Blueprint;
        public VMClock Clock { get; internal set; }

        private VMArchitecture _Arch;
        public VMArchitecture Architecture
        {
            get
            {
                return _Arch;
            }
            set
            {
                if (_Arch != null) _Arch.WallsChanged -= WallsChanged;
                value.WallsChanged += WallsChanged;
                _Arch = value;
            }
        }

        public World World { get; internal set; }
        public VMPrimitiveRegistration[] Primitives = new VMPrimitiveRegistration[256];
        public VMAmbientSound Ambience;
        public ulong RandomSeed;

        public GameGlobal Globals;
        public VMRoomInfo[] RoomInfo;
        private List<Dictionary<int, List<short>>> ObjectsAt; //used heavily for routing
        
        public VM VM;

        public VMContext(LotView.World world){
            this.World = world;
            this.Clock = new VMClock();
            this.Ambience = new VMAmbientSound();

            ObjectsAt = new List<Dictionary<int, List<short>>>();

            RandomSeed = (ulong)((new Random()).NextDouble() * UInt64.MaxValue); //when resuming state, this should be set.
            Clock.TicksPerMinute = 30; //1 minute per irl second

            AddPrimitive(new VMPrimitiveRegistration(new VMSleep())
            {
                Opcode = 0,
                Name = "sleep",
                OperandModel = typeof(VMSleepOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMGenericTSOCall())
            {
                Opcode = 1,
                Name = "generic_sims_online_call",
                OperandModel = typeof(VMGenericTSOCallOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMExpression())
            {
                Opcode = 2,
                Name = "expression",
                OperandModel = typeof(VMExpressionOperand)
            });

            //TODO: Report Metric

            AddPrimitive(new VMPrimitiveRegistration(new VMGrab())
            {
                Opcode = 4,
                Name = "grab",
                OperandModel = typeof(VMGrabOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMDrop())
            {
                Opcode = 5,
                Name = "drop",
                OperandModel = typeof(VMDropOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMChangeSuitOrAccessory())
            {
                Opcode = 6,
                Name = "change_suit_or_accessory",
                OperandModel = typeof(VMChangeSuitOrAccessoryOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMRefresh())
            {
                Opcode = 7,
                Name = "refresh",
                OperandModel = typeof(VMRefreshOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMRandomNumber())
            {
                Opcode = 8,
                Name = "random_number",
                OperandModel = typeof(VMRandomNumberOperand)
            });

            //TODO: burn

            //Sims 1.0 tutorial

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

            AddPrimitive(new VMPrimitiveRegistration(new VMBreakPoint())
            {
                Opcode = 15,
                Name = "breakpoint",
                OperandModel = typeof(VMBreakPointOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMFindLocationFor())
            {
                Opcode = 16,
                Name = "find_location_for",
                OperandModel = typeof(VMFindLocationForOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMIdleForInput())
            {
                Opcode = 17,
                Name = "idle_for_input",
                OperandModel = typeof(VMIdleForInputOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMRemoveObjectInstance())
            {
                Opcode = 18,
                Name = "remove_object_instance",
                OperandModel = typeof(VMRemoveObjectInstanceOperand)
            });

            //Make new character

            AddPrimitive(new VMPrimitiveRegistration(new VMRunFunctionalTree())
            {
                Opcode = 20,
                Name = "run_functional_tree",
                OperandModel = typeof(VMRunFunctionalTreeOperand)
            });

            //Show string: may be used but no functional result.

            AddPrimitive(new VMPrimitiveRegistration(new VMLookTowards())
            {
                Opcode = 22,
                Name = "look_towards",
                OperandModel = typeof(VMLookTowardsOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMPlaySound())
            {
                Opcode = 23,
                Name = "play_sound",
                OperandModel = typeof(VMPlaySoundOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMRelationship())
            {
                Opcode = 24,
                Name = "old_relationship",
                OperandModel = typeof(VMOldRelationshipOperand) //same primitive, different operand
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMTransferFunds())
            {
                Opcode = 25,
                Name = "transfer_funds",
                OperandModel = typeof(VMTransferFundsOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMRelationship())
            {
                Opcode = 26,
                Name = "relationship",
                OperandModel = typeof(VMRelationshipOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMGotoRelativePosition())
            {
                Opcode = 27,
                Name = "goto_relative",
                OperandModel = typeof(VMGotoRelativePositionOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMRunTreeByName())
            {
                Opcode = 28,
                Name = "run_tree_by_name",
                OperandModel = typeof(VMRunTreeByNameOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMSetMotiveChange())
            {
                Opcode = 29,
                Name = "set_motive_deltas",
                OperandModel = typeof(VMSetMotiveChangeOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMSysLog())
            {
                Opcode = 30,
                Name = "syslog",
                OperandModel = typeof(VMSysLogOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMSetToNext())
            {
                Opcode = 31,
                Name = "set_to_next",
                OperandModel = typeof(VMSetToNextOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMTestObjectType())
            {
                Opcode = 32,
                Name = "test_object_type",
                OperandModel = typeof(VMTestObjectTypeOperand)
            });

            //TODO: find 5 worst motives

            //TODO: ui effect (used?)

            AddPrimitive(new VMPrimitiveRegistration(new VMSpecialEffect())
            {
                Opcode = 35,
                Name = "special_effect",
                OperandModel = typeof(VMSpecialEffectOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMDialogPrivateStrings())
            {
                Opcode = 36,
                Name = "dialog_private",
                OperandModel = typeof(VMDialogOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMTestSimInteractingWith())
            {
                Opcode = 37,
                Name = "test_sim_interacting_with",
                OperandModel = typeof(VMTestSimInteractingWithOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMDialogGlobalStrings())
            {
                Opcode = 38,
                Name = "dialog_global",
                OperandModel = typeof(VMDialogOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMDialogSemiGlobalStrings())
            {
                Opcode = 39,
                Name = "dialog_semiglobal",
                OperandModel = typeof(VMDialogOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMOnlineJobsCall())
            {
                Opcode = 40,
                Name = "online_jobs_call",
                OperandModel = typeof(VMOnlineJobsCallOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMSetBalloonHeadline())
            {
                Opcode = 41,
                Name = "set_balloon_headline",
                OperandModel = typeof(VMSetBalloonHeadlineOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMCreateObjectInstance())
            {
                Opcode = 42,
                Name = "create_object_instance",
                OperandModel = typeof(VMCreateObjectInstanceOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMDropOnto())
            {
                Opcode = 43,
                Name = "drop_onto",
                OperandModel = typeof(VMDropOntoOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMAnimateSim()) {
                Opcode = 44,
                Name = "animate",
                OperandModel = typeof(VMAnimateSimOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMGotoRoutingSlot())
            {
                Opcode = 45,
                Name = "goto_routing_slot",
                OperandModel = typeof(VMGotoRoutingSlotOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMSnap()) //not functional right now
            {
                Opcode = 46,
                Name = "snap",
                OperandModel = typeof(VMSnapOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMReach())
            {
                Opcode = 47,
                Name = "reach",
                OperandModel = typeof(VMReachOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMStopAllSounds())
            {
                Opcode = 48,
                Name = "stop_all_sounds",
                OperandModel = typeof(VMStopAllSoundsOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMNotifyOutOfIdle())
            {
                Opcode = 49,
                Name = "stackobj_notify_out_of_idle",
                OperandModel = typeof(VMAnimateSimOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMChangeActionString())
            {
                Opcode = 50,
                Name = "change_action_string",
                OperandModel = typeof(VMChangeActionStringOperand)
            });

            //lots of unused primitives. see http://simantics.wikidot.com/wiki:primitives

            //TODO: Send Maxis Letter

            AddPrimitive(new VMPrimitiveRegistration(new VMInvokePlugin())
            {
                Opcode = 62,
                Name = "invoke_plugin",
                OperandModel = typeof(VMInvokePluginOperand)
            });

            AddPrimitive(new VMPrimitiveRegistration(new VMGetTerrainInfo())
            {
                Opcode = 63,
                Name = "get_terrain_info",
                OperandModel = typeof(VMGetTerrainInfoOperand)
            });

            //TODO: Get Terrain Info

            //UNUSED: Leave Lot and Goto

            AddPrimitive(new VMPrimitiveRegistration(new VMFindBestAction())
            {
                Opcode = 65,
                Name = "find_best_action",
                OperandModel = typeof(VMFindBestActionOperand)
            });

            //TODO: Set Dynamic Object Name
            
            //TODO: Inventory Operations

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

        private void WallsChanged(VMArchitecture caller)
        {
            RegeneratePortalInfo();

            //TODO: this could get very slow! find a way to make this quicker.
            foreach (var obj in VM.Entities)
            {
                if (obj is VMAvatar)
                {
                    foreach (var frame in obj.Thread.Stack)
                    {
                        if (frame is VMRoutingFrame)
                        {
                            ((VMRoutingFrame)frame).InvalidateRoomRoute();
                        }
                    }
                }
            }
        }

        public void RegeneratePortalInfo()
        {
            RoomInfo = new VMRoomInfo[Architecture.RoomData.Count()];
            for (int i = 0; i < RoomInfo.Length; i++)
            {
                RoomInfo[i].Entities = new List<VMEntity>();
                RoomInfo[i].Portals = new List<VMRoomPortal>();
                RoomInfo[i].Room = Architecture.RoomData[i];
            }

            foreach (var obj in VM.Entities)
            {
                var room = GetObjectRoom(obj);
                VM.AddToObjList(RoomInfo[room].Entities, obj);
                if (obj.EntryPoints[15].ActionFunction != 0)
                { //portal object
                    AddRoomPortal(obj, room);
                }
            }
        }

        public void AddRoomPortal(VMEntity obj, ushort room)
        {

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

        public void RemoveRoomPortal(VMEntity obj, ushort room)
        {
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
            if (pos.Level < 1) return;

            //add object to room

            var room = GetObjectRoom(obj);
            VM.AddToObjList(RoomInfo[room].Entities, obj);
            if (obj.EntryPoints[15].ActionFunction != 0)
            { //portal
                AddRoomPortal(obj, room);
            }

            while (pos.Level > ObjectsAt.Count) ObjectsAt.Add(new Dictionary<int, List<short>>());
            if (!ObjectsAt[pos.Level-1].ContainsKey(pos.TileID)) ObjectsAt[pos.Level - 1][pos.TileID] = new List<short>();
            ObjectsAt[pos.Level - 1][pos.TileID].Add(obj.ObjectID);
        }

        public void UnregisterObjectPos(VMEntity obj)
        {
            var pos = obj.Position;

            //remove object from room

            var room = GetObjectRoom(obj);
            RoomInfo[room].Entities.Remove(obj);
            if (obj.EntryPoints[15].ActionFunction != 0)
            { //portal
                RemoveRoomPortal(obj, room);
            }

            if (ObjectsAt[pos.Level - 1].ContainsKey(pos.TileID)) ObjectsAt[pos.Level - 1][pos.TileID].Remove(obj.ObjectID);
        }

        public bool CheckWallValid(LotTilePos pos, WallTile wall)
        {
            if (pos.Level < 1 || pos.Level > ObjectsAt.Count || !ObjectsAt[pos.Level - 1].ContainsKey(pos.TileID)) return true;
            var objs = ObjectsAt[pos.Level - 1][pos.TileID];
            foreach (var id in objs)
            {
                var obj = VM.GetObjectById(id);
                if (obj.WallChangeValid(wall, obj.Direction, false) != VMPlacementError.Success) return false;
            }
            return true;
        }

        public bool CheckFloorValid(LotTilePos pos, FloorTile floor)
        {
            if (pos.Level < 1 || pos.Level > ObjectsAt.Count || !ObjectsAt[pos.Level - 1].ContainsKey(pos.TileID)) return true;
            var objs = ObjectsAt[pos.Level - 1][pos.TileID];
            foreach (var id in objs)
            {
                var obj = VM.GetObjectById(id);
                if (obj.FloorChangeValid(floor, pos.Level) != VMPlacementError.Success) return false;
            }
            return true;
        }

        public VMSolidResult SolidToAvatars(LotTilePos pos)
        {
            if (IsOutOfBounds(pos) || (pos.Level < 1 || pos.Level > ObjectsAt.Count) || 
                (pos.Level != 1 && Architecture.GetFloor(pos.TileX, pos.TileY, pos.Level).Pattern == 0)) return new VMSolidResult { Solid = true };
            if (!ObjectsAt[pos.Level - 1].ContainsKey(pos.TileID)) return new VMSolidResult();
                var objs = ObjectsAt[pos.Level - 1][pos.TileID];
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
            return (pos.x < 0 || pos.y < 0 || pos.TileX >= _Arch.Width || pos.TileY >= _Arch.Height);
        }

        public VMPlacementResult GetAvatarPlace(VMEntity target, LotTilePos pos, Direction dir)
        {
            //avatars cannot be placed in slots under any circumstances, so we skip a few steps.

            VMObstacle footprint = target.GetObstacle(pos, dir);
            ushort room = GetRoomAt(pos);

            VMPlacementError status = VMPlacementError.Success;
            VMEntity statusObj = null;

            if (footprint == null || pos.Level < 1)
            {
                return new VMPlacementResult(status);
            }

            var objs = RoomInfo[room].Entities;
            foreach (var obj in objs)
            {
                if (obj.MultitileGroup == target.MultitileGroup) continue;
                var oFoot = obj.Footprint;

                if (oFoot != null && oFoot.Intersects(footprint)) //also ignore allow intersection trees?
                {
                    var flags = (VMEntityFlags)obj.GetValue(VMStackObjectVariable.Flags);
                    bool allowAvatars = ((flags & VMEntityFlags.DisallowPersonIntersection) == 0) && ((flags & VMEntityFlags.AllowPersonIntersection) > 0);
                    if (!allowAvatars)
                    {
                        status = VMPlacementError.CantIntersectOtherObjects;
                        statusObj = obj;
                        if (obj.EntryPoints[26].ActionFunction != 0) break; //select chairs immediately. 
                    }
                }

            }
            return new VMPlacementResult(status, statusObj);
        }

        public VMPlacementResult GetObjPlace(VMEntity target, LotTilePos pos, Direction dir)
        {
            //ok, this might be confusing...
            short allowedHeights = target.GetValue(VMStackObjectVariable.AllowedHeightFlags);
            short weight = target.GetValue(VMStackObjectVariable.Weight);
            bool noFloor = (allowedHeights&1)==0;

            var flags = (VMEntityFlags)target.GetValue(VMStackObjectVariable.Flags);
            bool allowAvatars = ((flags & VMEntityFlags.DisallowPersonIntersection) == 0) && ((flags & VMEntityFlags.AllowPersonIntersection) > 0);

            VMObstacle footprint = target.GetObstacle(pos, dir);
            ushort room = GetRoomAt(pos);

            VMPlacementError status = (noFloor)?VMPlacementError.HeightNotAllowed:VMPlacementError.Success;
            VMEntity statusObj = null;

            if (footprint == null || pos.Level < 1)
            {
                return new VMPlacementResult { Status = status };
            }

            var objs = RoomInfo[room].Entities;
            foreach (var obj in objs)
            {
                if (obj.MultitileGroup == target.MultitileGroup || (obj is VMAvatar && allowAvatars)) continue;
                var oFoot = obj.Footprint;

                if (oFoot != null && oFoot.Intersects(footprint)
                    && (!(target.ExecuteEntryPoint(5, this, true, obj, new short[] { obj.ObjectID, 0, 0, 0 })
                        || obj.ExecuteEntryPoint(5, this, true, target, new short[] { target.ObjectID, 0, 0, 0 })))
                    )
                {
                    statusObj = obj; 
                    status = VMPlacementError.CantIntersectOtherObjects;
                    
                    //this object is technically solid. Check if we can place on top of it
                    if (allowedHeights>1 && obj.TotalSlots() > 0 && (obj.GetSlot(0) == null || obj.GetSlot(0) == target))
                    {
                        //first check if we have a slot 0, which is what we place onto. then check if it's empty, 
                        //then check if the object can support this one's weight.
                        //we also need to make sure that the height of this specific slot is allowed.

                        if (((1 << (obj.GetSlotHeight(0) - 1)) & allowedHeights) > 0)
                        {
                            if (weight < obj.GetValue(VMStackObjectVariable.SupportStrength))
                            {
                                return new VMPlacementResult(VMPlacementError.Success, obj);
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
            return new VMPlacementResult(status, statusObj);
        }

        public ushort GetObjectRoom(VMEntity obj)
        {
            if (obj.Position == LotTilePos.OUT_OF_WORLD) return 0;
            if (obj.Position.Level < 1 || obj.Position.Level > _Arch.Stories) return 0;
            return Architecture.Rooms[obj.Position.Level - 1].Map[obj.Position.TileX + obj.Position.TileY*_Arch.Width];
        }

        public ushort GetRoomAt(LotTilePos pos)
        {
            if (pos.TileX < 0 || pos.TileX >= _Arch.Width) return 0;
            else if (pos.TileY < 0 || pos.TileY >= _Arch.Height) return 0;
            else if (pos.Level < 1 || pos.Level > _Arch.Stories) return 0;
            else return Architecture.Rooms[pos.Level-1].Map[pos.TileX + pos.TileY * _Arch.Width];
        }

        public VMMultitileGroup GhostCopyGroup(VMMultitileGroup group)
        {
            var newGroup = CreateObjectInstance(((group.MultiTile) ? group.BaseObject.MasterDefinition.GUID : group.BaseObject.Object.OBJ.GUID), LotTilePos.OUT_OF_WORLD, group.BaseObject.Direction, true);

            return newGroup;
        }

        public VMMultitileGroup CreateObjectInstance(UInt32 GUID, LotTilePos pos, Direction direction, bool ghostImage)
        {
            return CreateObjectInstance(GUID, pos, direction, 0, 0, ghostImage);
        }

        public VMMultitileGroup CreateObjectInstance(UInt32 GUID, LotTilePos pos, Direction direction)
        {
            return CreateObjectInstance(GUID, pos, direction, 0, 0, false);
        }

        public VMMultitileGroup CreateObjectInstance(UInt32 GUID, LotTilePos pos, Direction direction, short MainStackOBJ, short MainParam, bool ghostImage)
        {

            VMMultitileGroup group = new VMMultitileGroup();
            var objDefinition = FSO.Content.Content.Get().WorldObjects.Get(GUID);
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
                        var subObjDefinition = FSO.Content.Content.Get().WorldObjects.Get(objd[i].GUID);
                        if (subObjDefinition != null)
                        {
                            var worldObject = new ObjectComponent(subObjDefinition);
                            var vmObject = new VMGameObject(subObjDefinition, worldObject);
                            vmObject.GhostImage = ghostImage;

                            vmObject.MasterDefinition = objDefinition.OBJ;
                            vmObject.UseTreeTableOf(objDefinition);

                            vmObject.MainParam = MainParam;
                            vmObject.MainStackOBJ = MainStackOBJ;
                            group.Objects.Add(vmObject);

                            vmObject.MultitileGroup = group;
                            if (!ghostImage) VM.AddEntity(vmObject);
                            
                        }
                    }
                }

                group.Init(this);
                VMPlacementError couldPlace = group.ChangePosition(pos, direction, this).Status;
                return group;
            }
            else
            {
                if (objDefinition.OBJ.ObjectType == OBJDType.Person) //person
                {
                    var vmObject = new VMAvatar(objDefinition);
                    vmObject.MultitileGroup = group;
                    group.Objects.Add(vmObject);

                    vmObject.GhostImage = ghostImage;
                    if (!ghostImage) VM.AddEntity(vmObject);

                    if (UseWorld) Blueprint.AddAvatar((AvatarComponent)vmObject.WorldUI);

                    vmObject.MainParam = MainParam;
                    vmObject.MainStackOBJ = MainStackOBJ;

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

                    vmObject.GhostImage = ghostImage;
                    if (!ghostImage) VM.AddEntity(vmObject);

                    vmObject.MainParam = MainParam;
                    vmObject.MainStackOBJ = MainStackOBJ;

                    group.Init(this);
                    vmObject.SetPosition(pos, direction, this);
                    
                    return group;
                }
            }
        }

        public void RemoveObjectInstance(VMEntity target)
        {
            target.PrePositionChange(this);
            if (!target.GhostImage) VM.RemoveEntity(target);
            if (UseWorld)
            {
                if (target is VMGameObject) Blueprint.RemoveObject((ObjectComponent)target.WorldUI);
                else Blueprint.RemoveAvatar((AvatarComponent)target.WorldUI);
            }
        }

        public void AddPrimitive(VMPrimitiveRegistration primitive){
            Primitives[primitive.Opcode] = primitive;
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
        public VMEntity Object; //Container if above is .Success, Obstacle if above is any failure code.

        public VMPlacementResult(VMPlacementError status)
        {
            Status = status;
            Object = null;
        }

        public VMPlacementResult(VMPlacementError status, VMEntity obj)
        {
            Status = status;
            Object = obj;
        }
    }
}
