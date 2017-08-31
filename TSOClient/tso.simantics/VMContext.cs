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
using FSO.SimAntics.Marshals;
using FSO.LotView.LMap;

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
        public bool Ready { get { return (_Arch != null); } }

        public World World { get; internal set; }
        public VMPrimitiveRegistration[] Primitives = new VMPrimitiveRegistration[256];
        public VMAmbientSound Ambience;
        public ulong RandomSeed;

        public GameGlobal Globals;
        public TTAB GlobalTreeTable;
        public TTAs GlobalTTAs;
        public VMObjectQueries ObjectQueries;
        public VMRoomInfo[] RoomInfo;
        
        public VM VM;
        public bool DisableRouteInvalidation;

        public VMContext(LotView.World world) : this(world, null) { }

        public VMContext(LotView.World world, VMContext oldContext){
            //oldContext is passed in case we need to inherit certain things, like the ambient sound player
            this.World = world;
            this.Clock = new VMClock();
            this.ObjectQueries = new VMObjectQueries(this);

            if (oldContext == null)
            {
                this.Ambience = new VMAmbientSound();
            } else
            {
                this.Ambience = oldContext.Ambience;
            }

            Globals = FSO.Content.Content.Get().WorldObjectGlobals.Get("global");
            GlobalTreeTable = Globals.Resource.List<TTAB>()?.FirstOrDefault();
            GlobalTTAs = Globals.Resource.List<TTAs>()?.FirstOrDefault();
            RandomSeed = (ulong)((new Random()).NextDouble() * UInt64.MaxValue); //when resuming state, this should be set.

            AddPrimitive(new VMPrimitiveRegistration(new VMSleep())
            {
                Opcode = 0,
                Name = "sleep",
                OperandModel = typeof(VMSleepOperand)
            });


            //1 - generic tso call or generic ts1 call

            AddPrimitive(new VMPrimitiveRegistration(new VMExpression())
            {
                Opcode = 2,
                Name = "expression",
                OperandModel = typeof(VMExpressionOperand)
            });

            //TODO: Report Metric. TS1 - find best interaction

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

            AddPrimitive(new VMPrimitiveRegistration(new VMBurn())
            {
                Opcode = 9,
                Name = "burn",
                OperandModel = typeof(VMBurnOperand)
            });

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

            //TS1 - gosub found action
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

            AddPrimitive(new VMPrimitiveRegistration(new VMSnap())
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

            //UNUSED: Leave Lot and Goto

            AddPrimitive(new VMPrimitiveRegistration(new VMFindBestAction())
            {
                Opcode = 65,
                Name = "find_best_action",
                OperandModel = typeof(VMFindBestActionOperand)
            });

            //TODO: Set Dynamic Object Name

            //TODO: Inventory Operations
            AddPrimitive(new VMPrimitiveRegistration(new VMInventoryOperations())
            {
                Opcode = 67,
                Name = "inventory_operations",
                OperandModel = typeof(VMInventoryOperationsOperand)
            });

            if (Content.Content.Get().TS1)
            {
                AddPrimitive(new VMPrimitiveRegistration(new VMFindBestAction())
                {
                    Opcode = 3,
                    Name = "find_best_interaction",
                    OperandModel = typeof(VMFindBestActionOperand)
                });

                AddPrimitive(new VMPrimitiveRegistration(new VMGenericTS1Call())
                {
                    Opcode = 1,
                    Name = "generic_sims_call",
                    OperandModel = typeof(VMGenericTS1CallOperand)
                });

                AddPrimitive(new VMPrimitiveRegistration(new VMTS1Budget())
                {
                    Opcode = 25,
                    Name = "budget",
                    OperandModel = typeof(VMTransferFundsOperand)
                });

                AddPrimitive(new VMPrimitiveRegistration(new VMGosubFoundAction())
                {
                    Opcode = 30,
                    Name = "gosub_found_action",
                    OperandModel = typeof(VMGosubFoundActionOperand)
                });

                AddPrimitive(new VMPrimitiveRegistration(new VMTS1InventoryOperations())
                {
                    Opcode = 51,
                    Name = "manage_inventory",
                    OperandModel = typeof(VMTS1InventoryOperationsOperand)
                });
                Clock.TicksPerMinute = 30; //1 minute per irl second
            }
            else
            {
                Clock.TicksPerMinute = 30*5; //1 minute per 5 irl second
                AddPrimitive(new VMPrimitiveRegistration(new VMGenericTSOCall())
                {
                    Opcode = 1,
                    Name = "generic_sims_online_call",
                    OperandModel = typeof(VMGenericTSOCallOperand)
                });

                AddPrimitive(new VMPrimitiveRegistration(new VMTransferFunds())
                {
                    Opcode = 25,
                    Name = "transfer_funds",
                    OperandModel = typeof(VMTransferFundsOperand)
                });
            }
        }

        /// <summary>
        /// Returns a random number between 0 and less than the specified maximum.
        /// </summary>
        /// <param name="max">The upper bound of the random number.</param>
        /// <returns></returns>
        public ulong NextRandom(ulong max)
        {
            if (max == 0) return 0;
            RandomSeed ^= RandomSeed >> 12;
            RandomSeed ^= RandomSeed << 25;
            RandomSeed ^= RandomSeed >> 27;
            return (RandomSeed * (ulong)(2685821657736338717)) % max;
        }

        private void WallsChanged(VMArchitecture caller)
        {
            RegeneratePortalInfo();

            if (DisableRouteInvalidation) return;
            foreach (var obj in ObjectQueries.Avatars)
            {
                if (obj.Thread != null)
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
                RoomInfo[i].WindowPortals = new List<VMRoomPortal>();
                RoomInfo[i].Room = Architecture.RoomData[i];
                RoomInfo[i].Light = new RoomLighting();
            }

            foreach (var obj in VM.Entities)
            {
                var room = GetObjectRoom(obj);
                VM.AddToObjList(RoomInfo[room].Entities, obj);
                if (obj.EntryPoints[15].ActionFunction != 0)
                { //portal object
                    AddRoomPortal(obj, room);
                } else if (((VMEntityFlags2)obj.GetValue(VMStackObjectVariable.FlagField2)).HasFlag(VMEntityFlags2.ArchitectualWindow))
                {
                    AddWindowPortal(obj, room);
                }
                obj.SetRoom(room);
            }

            var visited = new HashSet<ushort>();
            for (ushort i=0; i<RoomInfo.Length; i++)
            {
                RefreshLighting(i, i==(RoomInfo.Length-1), visited);
            }
            if (VM.UseWorld) World.InvalidateZoom();
        }

        public void RefreshRoomScore(ushort room)
        {
            if (RoomInfo == null || room == 0) return;
            var info = RoomInfo[room];
            room = info.Room.LightBaseRoom;
            info = RoomInfo[room];

            if (info.Light == null) info.Light = new RoomLighting();
            else info.Light.RoomScore = 0;
            var light = info.Light;

            var area = 0;
            var roomScore = 0;
            foreach (var rm in info.Room.SupportRooms)
            {
                info = RoomInfo[rm];
                if (info.Light == null) info.Light = light; //adjacent rooms share a light object.
                area += info.Room.Area;
                foreach (var ent in info.Entities)
                {
                    var roomImpact = ent.GetValue(VMStackObjectVariable.RoomImpact);
                    if (roomImpact != 0) roomScore += roomImpact;
                }
            }

            float areaRScale = Math.Max(1, area / 12f);
            if (info.Room.IsOutside) areaRScale = 30;
            roomScore = (short)(roomScore / areaRScale);
            roomScore -= (info.Room.IsOutside) ? 15 : 10;

            light.RoomScore = (short)Math.Min(100, Math.Max(-100, roomScore));
        }

        public void RefreshLighting(ushort room, bool commit, HashSet<ushort> visited)
        {
            if (RoomInfo == null || room == 0) return;
            var info = RoomInfo[room];
            var isoutside = info.Room.IsOutside;
            room = info.Room.LightBaseRoom;
            if (visited.Contains(room)) return;
            visited.Add(room);
            info = RoomInfo[room];
            var light = new RoomLighting();
            RoomInfo[room].Light = light;
            light.Bounds = info.Room.Bounds;
            light.AmbientLight = 0;
            var affected = new HashSet<ushort>();

            var area = 0;
            var outside = 0;
            var inside = 0;
            var roomScore = 0;
            var useWorld = UseWorld;
            foreach (var rm in info.Room.SupportRooms)
            {
                info = RoomInfo[rm];
                light.Bounds = Rectangle.Union(light.Bounds, info.Room.Bounds);
                RoomInfo[room].Light = light; //adjacent rooms share a light object.
                area += info.Room.Area;
                foreach (var ent in info.Entities)
                {
                    if (ent.MultitileGroup.Objects.Count == 0) continue;
                    var mainSource = ent == ent.MultitileGroup.Objects[0];
                    var flags2 = (VMEntityFlags2)ent.GetValue(VMStackObjectVariable.FlagField2);
                    
                    var cont = ent.GetValue(VMStackObjectVariable.LightingContribution);
                    if (cont > 0)
                    {
                        if ((flags2 & (VMEntityFlags2.ArchitectualWindow | VMEntityFlags2.ArchitectualDoor)) > 0)
                        {
                            if (true) light.Lights.Add(new LotView.LMap.LightData(new Vector2(ent.Position.x, ent.Position.y), true, 160));
                            outside += (ushort)cont;
                        }
                        else
                        {
                            if (mainSource) light.Lights.Add(new LotView.LMap.LightData(new Vector2(ent.Position.x, ent.Position.y), false, 160));
                            inside += (ushort)cont;
                        }
                    }
                    else if (mainSource && useWorld)
                    {
                        var bound = ent.MultitileGroup.LightBounds();
                        if (bound != null) light.ObjectFootprints.Add(bound.Value);
                    }
                    var roomImpact = ent.GetValue(VMStackObjectVariable.RoomImpact);
                    if (roomImpact != 0) roomScore += roomImpact;
                }

                foreach (var portal in info.WindowPortals)
                {
                    var ent = VM.GetObjectById(portal.ObjectID);
                    var wlight = new LotView.LMap.LightData(new Vector2(ent.Position.x, ent.Position.y), false, 100);
                    wlight.WindowRoom = portal.TargetRoom;
                    var bRoom = RoomInfo[portal.TargetRoom].Room.LightBaseRoom;
                    affected.Add(bRoom);
                    light.Lights.Add(wlight);
                }

            }

            float areaScale = Math.Max(1, area / 100f);
            LightData.Cluster(light.Lights);
            light.OutsideLight = Math.Min((ushort)100, (ushort)(outside / areaScale));
            light.AmbientLight = Math.Min((ushort)100, (ushort)(inside / areaScale));

            if (info.Room.IsOutside)
            {
                light.AmbientLight = 0;
                light.OutsideLight = 100;
            }

            float areaRScale = Math.Max(1, area / 12f);
            if (info.Room.IsOutside) areaRScale = 30;
            roomScore = (short)(roomScore / areaRScale);
            roomScore -= (info.Room.IsOutside) ? 15 : 10;

            light.RoomScore = (short)Math.Min(100, Math.Max(-100, roomScore));

            if (commit && useWorld)
            {
                Blueprint.Light = new RoomLighting[RoomInfo.Length];
                for (int i = 0; i < RoomInfo.Length; i++)
                {
                    Blueprint.Light[i] = RoomInfo[i].Light;
                }
                Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.LIGHTING_CHANGED, (short)room, 0, 0));
                foreach (var a in affected)
                {
                    Blueprint.Damage.Add(new BlueprintDamage(BlueprintDamageType.LIGHTING_CHANGED, (short)a, 0, 0));
                }
            }
        }

        public void AddRoomPortal(VMEntity obj, ushort room)
        {
            if (obj.MultitileGroup == null) return;
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
        public void AddWindowPortal(VMEntity obj, ushort room)
        {
            if (obj.MultitileGroup == null) return;
            //find other portal part, must be in other room to count...
            foreach (var obj2 in obj.MultitileGroup.Objects)
            {
                var room2 = GetObjectRoom(obj2);
                if (obj != obj2 && room2 != room)
                {
                    RoomInfo[room].WindowPortals.Add(new VMRoomPortal(obj.ObjectID, room2));
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

        public void RemoveWindowPortal(VMEntity obj, ushort room)
        {
            VMRoomPortal target = null;
            foreach (var port in RoomInfo[room].WindowPortals)
            {
                if (port.ObjectID == obj.ObjectID)
                {
                    target = port;
                    break;
                }
            }
            if (target != null) RoomInfo[room].WindowPortals.Remove(target);
        }

        /// <summary>
        /// Regenerates room obstacles for a specific room. These are the obstacles preventing routing from walking the sim into another room.
        /// They are removed by door portals, so whenever they are moved they need to be regenerated for both rooms the portal is between.
        /// </summary>
        /// <param name="room"></param>
        /// <param name="level"></param>
        public void RegenRoomObs(ushort room, sbyte level)
        {
            RoomInfo[room].Room.RoomObs = Architecture.Rooms[level - 1].GenerateRoomObs(room, level, RoomInfo[room].Room.Bounds, this);
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
                RegenRoomObs(room, obj.Position.Level); //the other portal side will call this on the other room, which is what we really affect.
            } else if (((VMEntityFlags2)obj.GetValue(VMStackObjectVariable.FlagField2)).HasFlag(VMEntityFlags2.ArchitectualWindow))
            {
                AddWindowPortal(obj, room);
            }
            obj.SetRoom(room);
            if (obj.GetValue(VMStackObjectVariable.LightingContribution) > 0)
                RefreshLighting(room, true, new HashSet<ushort>());
            else if (obj.GetValue(VMStackObjectVariable.RoomImpact) > 0)
                RefreshRoomScore(room);

            ObjectQueries.RegisterObjectPos(obj);
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
                RegenRoomObs(room, obj.Position.Level); //the other portal side will call this on the other room, which is what we really affect.
            }
            else if (((VMEntityFlags2)obj.GetValue(VMStackObjectVariable.FlagField2)).HasFlag(VMEntityFlags2.ArchitectualWindow))
                RemoveWindowPortal(obj, room);
            if (obj.GetValue(VMStackObjectVariable.LightingContribution) > 0)
                RefreshLighting(room, true, new HashSet<ushort>());
            else if (obj.GetValue(VMStackObjectVariable.RoomImpact) > 0)
                RefreshRoomScore(room);

            ObjectQueries.UnregisterObjectPos(obj);
        }

        public bool SlopeVertexCheck(int x, int y)
        {
            for (sbyte i = 1; i <= 5; i++)
            {
                var pos = LotTilePos.FromBigTile((short)x, (short)y, i);
                if (!CheckSlopeValid(pos)) return false;
                pos.x -= 16;
                if (!CheckSlopeValid(pos)) return false;
                pos.y -= 16;
                if (!CheckSlopeValid(pos)) return false;
                pos.x += 16;
                if (!CheckSlopeValid(pos)) return false;
            }
            return true;
        }

        public bool CheckSlopeValid(LotTilePos pos)
        {
            if (pos.x < 0 || pos.y < 0) return true;
            var objs = ObjectQueries.GetObjectsAt(pos);
            if (objs != null)
            {
                foreach (var obj in objs)
                {
                    if (obj.SlopeValid() != VMPlacementError.Success) return false;
                }
            }
            if (Architecture.GetWall(pos.TileX, pos.TileY, pos.Level).Segments != 0) return false;
            return true;
        }

        public bool CheckWallValid(LotTilePos pos, WallTile wall)
        {
            if (wall.Segments > 0 && Architecture.GetTerrainSloped(pos.TileX, pos.TileY)) return false;
            var objs = ObjectQueries.GetObjectsAt(pos);
            if (objs == null) return true;
            foreach (var obj in objs)
            {
                if (obj.WallChangeValid(wall, obj.Direction, false) != VMPlacementError.Success) return false;
            }
            return true;
        }

        public bool CheckFloorValid(LotTilePos pos, FloorTile floor)
        {
            var objs = ObjectQueries.GetObjectsAt(pos);
            if (objs == null) return true;
            foreach (var obj in objs)
            {
                if (obj.FloorChangeValid(floor.Pattern, pos.Level) != VMPlacementError.Success) return false;
            }
            return true;
        }

        public VMSolidResult SolidToAvatars(LotTilePos pos)
        {
            if (IsOutOfBounds(pos) || (pos.Level < 1) || 
                (pos.Level != 1 && Architecture.GetFloor(pos.TileX, pos.TileY, pos.Level).Pattern == 0)) return new VMSolidResult { Solid = true };
            var objs = ObjectQueries.GetObjectsAt(pos);
            if (objs == null) return new VMSolidResult();
            foreach (var obj in objs)
            {
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
            return (pos.x < 0 || pos.y < 0 || pos.Level < 1 || pos.TileX >= _Arch.Width || pos.TileY >= _Arch.Height || pos.Level > _Arch.Stories);
        }

        /// <summary>
        /// Returns if the area is "out of bounds" for user placement.
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public bool IsUserOutOfBounds(LotTilePos pos)
        {
            var area = Architecture.BuildableArea;
            return (pos.TileX < area.X || pos.TileY < area.Y || pos.Level < 1 || pos.TileX >= area.Right || pos.TileY >= area.Bottom || pos.Level > _Arch.BuildableFloors);
        }

        public void UpdateTSOBuildableArea()
        {
            VMBuildableAreaInfo.UpdateOverbudgetObjects(VM);
            var lotSInfo = VM.TSOState.Size;
            var area = GetTSOBuildableArea(lotSInfo);
            Architecture.UpdateBuildableArea(area, ((lotSInfo >> 8) & 255) + 2);
        }

        public Rectangle GetTSOBuildableArea(int lotSInfo)
        {
            //note: sync on this DOES matter as the OOB check performs on some primitives, and objects are double checked before placement.

            var lotSize = lotSInfo & 255;
            var lotFloors = ((lotSInfo >> 8)&255)+2;
            var lotDir = (lotSInfo >> 16);

            var dim = VMBuildableAreaInfo.BuildableSizes[lotSize];

            //need to rotate the lot dir towards the road. bit weird cos we're rotating a rectangle

            var w = Architecture.Width;
            var h = Architecture.Height;
            var corners = new Vector2[]
            {
                new Vector2(6, 6), // top, default orientation
                new Vector2(w-7, 6), // right
                new Vector2(w-7, h-7), // bottom
                new Vector2(6, h-7) // left
            };
            var perpIncrease = new Vector2[]
            {
                new Vector2(0, -1), //bottom left road side
                new Vector2(-1, 0),
                new Vector2(0, -1),
                new Vector2(-1, 0)
            };

            //rotation 0: move perp from closer point to top bottom -> left (90 degree ccw of perp)
            //rotation 1: choose closer pt to top left->top (90 degree ccw of perp)
            //rotation 2: choose closer pt to top top->right (90 degree cw of perp)

            var pt1 = corners[(lotDir + 2) % 4];
            var pt2 = corners[(lotDir + 3) % 4];

            var ctr = (pt1 + pt2) / 2;
            var lotBase = ctr + perpIncrease[(3 - lotDir) % 4]*dim/2;
            if (lotDir == 0 || lotDir == 3) lotBase += perpIncrease[lotDir]*(dim);

            return new Rectangle((int)lotBase.X, (int)lotBase.Y, dim, dim);
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
            var meAllowAvatars = target.GetFlag(VMEntityFlags.AllowPersonIntersection);
            foreach (var obj in objs)
            {
                if (obj.MultitileGroup == target.MultitileGroup) continue;
                var oFoot = obj.Footprint;

                if (oFoot != null && oFoot.Intersects(footprint)
                        && (!(target.ExecuteEntryPoint(5, this, true, obj, new short[] { obj.ObjectID, 0, 0, 0 })
                        || obj.ExecuteEntryPoint(5, this, true, target, new short[] { target.ObjectID, 0, 0, 0 })))
                    )
                {
                    var flags = (VMEntityFlags)obj.GetValue(VMStackObjectVariable.Flags);
                    bool allowAvatars = (obj is VMAvatar && meAllowAvatars) || 
                        (((flags & VMEntityFlags.DisallowPersonIntersection) == 0) && ((flags & VMEntityFlags.AllowPersonIntersection) > 0));
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
                if (obj.MultitileGroup == target.MultitileGroup || (obj is VMAvatar && allowAvatars) 
                    || (target.IgnoreIntersection != null && target.IgnoreIntersection.Objects.Contains(obj))) continue;
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
            return GetRoomAt(obj.Position);
        }

        public ushort GetRoomAt(LotTilePos pos)
        {
            if (pos.TileX < 0 || pos.TileX >= _Arch.Width) return 0;
            else if (pos.TileY < 0 || pos.TileY >= _Arch.Height) return 0;
            else if (pos.Level < 1 || pos.Level > _Arch.Stories) return 0;
            else
            {
                uint tileRoom = Architecture.Rooms[pos.Level - 1].Map[pos.TileX + pos.TileY * _Arch.Width];
                var room2 = ((tileRoom >> 16) & 0x7FFF);
                if ((tileRoom & 0xFFFF) != room2)
                {
                    var walls = _Arch.GetWall(pos.TileX, pos.TileY, pos.Level);

                    if ((walls.Segments & WallSegments.VerticalDiag) > 0) 
                    {
                        if ((pos.x % 16) - (pos.y % 16) > 0)
                            return (ushort)tileRoom;
                        else
                            return (ushort)room2;
                    }
                    else if ((walls.Segments & WallSegments.HorizontalDiag) > 0)
                    {
                        if ((pos.x % 16) + (pos.y % 16) > 15)
                            return (ushort)room2;
                        else
                            return (ushort)tileRoom;
                    }
                }
                return (ushort)tileRoom;
            }
        }

        public short GetRoomScore(ushort room)
        {
            if (room >= RoomInfo.Length) return 0;
            return RoomInfo[room].Light.RoomScore;
        }

        public VMMultitileGroup GhostCopyGroup(VMMultitileGroup group)
        {
            var newGroup = CreateObjectInstance(((group.MultiTile) ? group.BaseObject.MasterDefinition.GUID : group.BaseObject.Object.OBJ.GUID), LotTilePos.OUT_OF_WORLD, group.BaseObject.Direction, true);

            if (newGroup != null)
            {
                newGroup.Price = group.Price;
                for (int i=0; i < Math.Min(newGroup.Objects.Count, group.Objects.Count); i++) {
                    newGroup.Objects[i].IgnoreIntersection = group;
                    newGroup.Objects[i].SetValue(VMStackObjectVariable.Graphic, group.Objects[i].GetValue(VMStackObjectVariable.Graphic));
                    newGroup.Objects[i].DynamicSpriteFlags = group.Objects[i].DynamicSpriteFlags;
                    newGroup.Objects[i].DynamicSpriteFlags2 = group.Objects[i].DynamicSpriteFlags2;
                    newGroup.Objects[i].SetDynamicSpriteFlag(0, group.Objects[i].IsDynamicSpriteFlagSet(0));
                    newGroup.Objects[i].PlatformState = group.Objects[i].PlatformState;
                    if (newGroup.Objects[i] is VMGameObject) ((VMGameObject)newGroup.Objects[i]).RefreshGraphic();
                }
            }

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

            var catalog = Content.Content.Get().WorldCatalog;
            var item = catalog.GetItemByGUID(GUID);

            int salePrice = 0;
            if (item != null) salePrice = (int)item.Value.Price;
            salePrice = Math.Max(0, Math.Min(salePrice, (salePrice * (100 - objDefinition.OBJ.InitialDepreciation)) / 100));

            group.Price = (int)salePrice;

            var master = objDefinition.OBJ.MasterID;
            if (master != 0 && objDefinition.OBJ.SubIndex == -1)
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
                            var worldObject = MakeObjectComponent(subObjDefinition);
                            var vmObject = new VMGameObject(subObjDefinition, worldObject);
                            vmObject.GhostImage = ghostImage;
                            if (UseWorld) Blueprint.AddObject(worldObject);

                            vmObject.MasterDefinition = objDefinition.OBJ;
                            vmObject.UseTreeTableOf(objDefinition);

                            vmObject.MainParam = MainParam;
                            vmObject.MainStackOBJ = MainStackOBJ;
                            group.AddObject(vmObject);

                            vmObject.MultitileGroup = group;
                            if (!ghostImage) VM.AddEntity(vmObject);
                            
                        }
                    }
                }

                group.Init(this);
                VMPlacementError couldPlace = group.ChangePosition(pos, direction, this, VMPlaceRequestFlags.Default).Status;
                return group;
            }
            else
            {
                if (objDefinition.OBJ.ObjectType == OBJDType.Person) //person
                {
                    var vmObject = new VMAvatar(objDefinition);
                    vmObject.MultitileGroup = group;
                    group.AddObject(vmObject);

                    vmObject.GhostImage = ghostImage;
                    if (!ghostImage) VM.AddEntity(vmObject);

                    if (UseWorld) Blueprint.AddAvatar((AvatarComponent)vmObject.WorldUI);

                    vmObject.MainParam = MainParam;
                    vmObject.MainStackOBJ = MainStackOBJ;

                    group.Init(this);
                    vmObject.SetPosition(pos, direction, this);

                    if (VM.TS1)
                    {
                        var id = Content.Content.Get().Neighborhood.GetNeighborIDForGUID(GUID);
                        if (id != null)
                        {
                            var neigh = Content.Content.Get().Neighborhood.GetNeighborByID(id.Value);
                            if (neigh != null) vmObject.InheritNeighbor(neigh);
                        }
                    }
                 
                    return group;
                }
                else
                {
                    var worldObject = MakeObjectComponent(objDefinition);
                    var vmObject = new VMGameObject(objDefinition, worldObject);

                    vmObject.MultitileGroup = group;

                    group.AddObject(vmObject);

                    vmObject.GhostImage = ghostImage;
                    if (!ghostImage) VM.AddEntity(vmObject);
                    if (UseWorld && Blueprint != null) Blueprint.AddObject(worldObject);

                    vmObject.MainParam = MainParam;
                    vmObject.MainStackOBJ = MainStackOBJ;

                    group.Init(this);
                    var result = vmObject.SetPosition(pos, direction, this);
                    
                    return group;
                }
            }
        }

        public void RemoveObjectInstance(VMEntity target)
        {
            target.PrePositionChange(this);
            if (!target.GhostImage)
            {
                VM.RemoveEntity(target);
            }
            if (UseWorld)
            {
                if (target is VMGameObject) Blueprint.RemoveObject((ObjectComponent)target.WorldUI);
                else Blueprint.RemoveAvatar((AvatarComponent)target.WorldUI);
            }
            if (VM.EODHost != null)
            {
                VM.EODHost.ForceDisconnectObj(target);
            }
        }

        public ObjectComponent MakeObjectComponent(GameObject obj)
        {
            if (UseWorld) return World.MakeObjectComponent(obj);
            return new ObjectComponent(obj);
        }

        public void AddPrimitive(VMPrimitiveRegistration primitive){
            Primitives[primitive.Opcode] = primitive;
        }

        #region VM Marshalling Functions
        public virtual VMContextMarshal Save()
        {
            return new VMContextMarshal
            {
                Architecture = Architecture.Save(),
                Clock = Clock.Save(),
                Ambience = new VMAmbientSoundMarshal { ActiveBits = Ambience.ActiveBits },
                RandomSeed = RandomSeed
            };
        }

        public virtual void Load(VMContextMarshal input)
        {
            if (VM.UseWorld) Blueprint = new Blueprint(input.Architecture.Width, input.Architecture.Height);
            Architecture = new VMArchitecture(input.Architecture, this, Blueprint);
            Clock = new VMClock(input.Clock);

            for (int i=0; i<VMAmbientSound.SoundByBitField.Count; i++) Ambience.SetAmbience((byte)i, (input.Ambience.ActiveBits&((ulong)1<<i)) > 0);

            if (VM.UseWorld)
            {
                World.State.WorldSize = input.Architecture.Width;
                Blueprint.Terrain = new TerrainComponent(new Rectangle(0, 0, input.Architecture.Width, input.Architecture.Height), Blueprint);
                Blueprint.Terrain.Initialize(this.World.State.Device, this.World.State);

                World.InitBlueprint(Blueprint);
            }
           
            RandomSeed = input.RandomSeed;
        }

        public VMContext(VMContextMarshal input, VMContext oldContext) : this(oldContext.World, oldContext)
        {
            var oldBlueprint = (oldContext == null)?null:oldContext.Blueprint;
            Load(input);
            if (oldBlueprint != null) Blueprint.SubWorlds = oldBlueprint.SubWorlds;
        }
        #endregion
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
