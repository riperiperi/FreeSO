/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using FSO.Files.Formats.IFF.Chunks;
using FSO.LotView.Model;
using FSO.Common.Utils;
using FSO.SimAntics.Model.Routing;
using FSO.SimAntics.NetPlay.Model;
using System.IO;
using FSO.SimAntics.Marshals.Threads;
using FSO.SimAntics.Model;

namespace FSO.SimAntics.Engine
{
    public class VMSlotParser
    {
        public const double ANGLE_ERROR = -0.1f;

        public List<VMFindLocationResult> Results;
        public VMRouteFailCode FailCode = VMRouteFailCode.NoValidGoals;
        public VMEntity Blocker = null;

        public SLOTItem Slot;

        private static VMRouteFailCode[] FailPrio = {
            VMRouteFailCode.NoValidGoals,
            VMRouteFailCode.NoChair,
            VMRouteFailCode.DestTileOccupiedPerson,
            VMRouteFailCode.DestTileOccupied,
        };

        private SLOTFlags Flags;
        private int MinProximity;
        private int MaxProximity;
        private int DesiredProximity;

        private bool OnlySit
        {
            get
            {
                return (Slot.Sitting > 0 && Slot.Standing == 0);
            }
        }

        public VMSlotParser(SLOTItem slot)
        {
            Slot = slot;
            Flags = slot.Rsflags;

            MinProximity = slot.MinProximity;
            MaxProximity = slot.MaxProximity;
            DesiredProximity = slot.OptimalProximity;


            if (MaxProximity == 0) { MaxProximity = MinProximity; }
            if (DesiredProximity == 0) { DesiredProximity = MinProximity; }
        }

        // TODO: float values may desync if devices are not both x86 or using a different C# library. 
        // Might need to replace with fixed point library for position and rotation

        /// <summary>
        /// This method will find all the avaliable locations within the criteria ordered by proximity to the optimal proximity
        /// External functions can then decide which is most desirable. E.g. the nearest slot to the object may be the longest route if
        /// its in another room.
        /// </summary>
        /// <param name="obj"></param>
        /// <param name="slot"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public List<VMFindLocationResult> FindAvaliableLocations(VMEntity obj, VMContext context, VMEntity caller)
        {
            /**
             * Start at min proximity and circle around the object to find the avaliable locations.
             * Then pick the one nearest to the optimal value
             */

            /**
             * ------ MAJOR TODO: ------
             * Avoid vector math at all costs! Small differences in hardware could cause desyncs.
             * This really goes for all areas of the SimAntics engine, but here it's particularly bad. 
             */
            Vector2 center;
            if (OnlySit) FailCode = VMRouteFailCode.NoChair;

            // if we need to use the average location of an object group, it needs to be calculated.
            if (((Flags & SLOTFlags.UseAverageObjectLocation) > 0) && (obj.MultitileGroup.MultiTile)) {
                center = new Vector2(0, 0);
                var objs = obj.MultitileGroup.Objects;
                for (int i = 0; i < objs.Count; i++)
                {
                    center += new Vector2(objs[i].Position.x/16f, objs[i].Position.y/16f);
                }
                center /= objs.Count;
            } else center = new Vector2(obj.Position.x/16f, obj.Position.y/16f);

            //add offset of slot if it exists. must be rotated to be relative to object
            var rotOff = Vector3.Transform(Slot.Offset, Matrix.CreateRotationZ(obj.RadianDirection));
            var circleCtr = new Vector2(center.X + rotOff.X / 16, center.Y + rotOff.Y / 16);

            ushort room = context.VM.Context.GetRoomAt(obj.Position);
            Results = new List<VMFindLocationResult>();

            if ((Flags & SLOTFlags.SnapToDirection) > 0)
            { //snap to the specified direction, on the specified point.
                double baseRot;
                if (Slot.Facing > SLOTFacing.FaceAwayFromObject)
                {
                    // bit of a legacy thing here. Facing field did not use to exist,
                    // which is why SnapToDirection was hacked to use the directional flags.
                    // now that it exists, it is used instead, to encode the same information...
                    // just transform back into old format.
                    Flags |= (SLOTFlags)(1 << (int)Slot.Facing);
                }
                else
                {
                    if (((int)Flags & 255) == 0) Flags |= SLOTFlags.NORTH;
                }

                var flagRot = DirectionUtils.PosMod(obj.RadianDirection+FlagsAsRad(Flags), Math.PI*2);
                if (flagRot > Math.PI) flagRot -= Math.PI * 2;

                VerifyAndAddLocation(obj, circleCtr, center, Flags, Double.MaxValue, context, caller, (float)flagRot); 
                return Results;
            }
            else
            {
                if (((int)Flags & 255) == 0 || Slot.Offset != new Vector3())
                {
                    //exact position
                    //Flags |= (SLOTFlags)255;

                    // special case, walk directly to point. 
                    VerifyAndAddLocation(obj, circleCtr, center, Flags, Double.MaxValue, context, caller, float.NaN);
                    return Results;
                }
                var maxScore = Math.Max(DesiredProximity - MinProximity, MaxProximity - DesiredProximity) + (LotTilePos.Distance(obj.Position, caller.Position)+MaxProximity)/3 + 2;
                var ignoreRooms = (Flags & SLOTFlags.IgnoreRooms) > 0;

                SLOTEnumerationFunction((x, y, distance) =>
                {
                    var pos = new Vector2(circleCtr.X + x / 16.0f, circleCtr.Y + y / 16.0f);
                    if (distance >= MinProximity - 0.5 && distance <= MaxProximity + 0.5 && (ignoreRooms || context.VM.Context.GetRoomAt(new LotTilePos((short)Math.Round(pos.X * 16), (short)Math.Round(pos.Y * 16), obj.Position.Level)) == room)) //slot is within proximity
                    {
                        var routeEntryFlags = (GetSearchDirection(circleCtr, pos, obj.RadianDirection) & Flags); //the route needs to know what conditions it fulfilled
                        if (routeEntryFlags > 0) //within search location
                        {
                            double baseScore = ((maxScore - Math.Abs(DesiredProximity - distance)) + context.VM.Context.NextRandom(1024) / 1024.0f);
                            VerifyAndAddLocation(obj, pos, center, routeEntryFlags, baseScore, context, caller, float.NaN);
                        }
                    }
                });
            }
            /** Sort by how close they are to desired proximity **/
            
            if (Results.Count > 1) Results = Results.OrderBy(x => -x.Score).ToList(); //avoid sort because it acts incredibly unusually
            if (Results.Count > 0) FailCode = VMRouteFailCode.Success;
            return Results;
        }

        private void SLOTEnumerationFunction (Callback<int, int, double> outputFunc)
        {
            if (Slot.MaxProximity == Slot.MinProximity)
            {
                //circle. pick points in the 8 directions to check.
                for (int i = 0; i < 8; i++)
                {
                    var angle = (i * Math.PI) / 4.0;
                    var x = (int)Math.Round(Math.Sin(angle) * Slot.MinProximity); //most directions shouldn't cause floating point issues here.
                    var y = (int)Math.Round(Math.Cos(angle) * Slot.MinProximity);
                    outputFunc(x, y, Slot.MinProximity);
                }
            }
            else
            {
                //range. use the resolution settings.
                var resolutionBound = (MaxProximity / Slot.Resolution) * Slot.Resolution;

                for (int x = -resolutionBound; x <= resolutionBound; x += Slot.Resolution)
                {
                    for (int y = -resolutionBound; y <= resolutionBound; y += Slot.Resolution)
                    {
                        double distance = Math.Sqrt(x * x + y * y);
                        outputFunc(x, y, distance);
                    }
                }
            }
        }

        private void VerifyAndAddLocation(VMEntity obj, Vector2 pos, Vector2 center, SLOTFlags entryFlags, double score, VMContext context, VMEntity caller, float facingDir)
        {
            //note: verification is not performed if snap target slot is enabled.
            var tpos = new LotTilePos((short)Math.Round(pos.X * 16), (short)Math.Round(pos.Y * 16), obj.Position.Level);

            if (context.IsOutOfBounds(tpos)) return;

            score -= LotTilePos.Distance(tpos, caller.Position)/3.0;

            if (Slot.SnapTargetSlot < 0 && context.Architecture.RaycastWall(new Point((int)pos.X, (int)pos.Y), new Point(obj.Position.TileX, obj.Position.TileY), obj.Position.Level))
            {
                SetFail(VMRouteFailCode.WallInWay, null);
                return;
            } 

            bool faceAnywhere = false;
            if (float.IsNaN(facingDir))
            {
                var obj3P = obj.Position.ToVector3();
                var objP = new Vector2(obj3P.X, obj3P.Y);
                switch (Slot.Facing)
                {
                    case SLOTFacing.FaceTowardsObject:
                        facingDir = (float)GetDirectionTo(pos, objP); break;
                    case SLOTFacing.FaceAwayFromObject:
                        facingDir = (float)GetDirectionTo(objP, pos); break;
                    case SLOTFacing.FaceAnywhere:
                        faceAnywhere = true;
                        facingDir = 0.0f; break;
                    default:
                        int intDir = (int)Math.Round(Math.Log((double)obj.Direction, 2));
                        var rotatedF = ((int)Slot.Facing + intDir) % 8;
                        facingDir = (float)(((int)rotatedF > 4) ? ((double)rotatedF * Math.PI / 4.0) : (((double)rotatedF - 8.0) * Math.PI / 4.0)); break;
                }
            }

            VMFindLocationResult result = new VMFindLocationResult
            {
                Position = new LotTilePos((short)Math.Round(pos.X * 16), (short)Math.Round(pos.Y * 16), obj.Position.Level),
                RadianDirection = facingDir,
                FaceAnywhere = faceAnywhere,
                RouteEntryFlags = entryFlags
            };
            var avatarInWay = false;
            if (Slot.SnapTargetSlot < 0)
            {
                var solid = caller.PositionValid(tpos, Direction.NORTH, context, VMPlaceRequestFlags.AcceptSlots);
                if (solid.Status != Model.VMPlacementError.Success)
                {
                    if (solid.Object != null)
                    {
                        if (solid.Object is VMGameObject)
                        {
                            if (Slot.Sitting > 0 && solid.Object.EntryPoints[26].ActionFunction != 0)
                            {
                                result.Chair = solid.Object;
                            }
                            else
                            {
                                SetFail(VMRouteFailCode.DestTileOccupied, solid.Object);
                                return;
                            }
                        } else
                        {
                            avatarInWay = true;
                        }
                    } 
                }

                if (context.ObjectQueries.GetObjectsAt(tpos)?.Any(
                    x => ((VMEntityFlags2)x.GetValue(VMStackObjectVariable.FlagField2) & VMEntityFlags2.ArchitectualDoor) > 0) ?? false)
                    avatarInWay = true; //prefer not standing in front of a door. (todo: merge with above check?)

                if (result.Chair != null && (Math.Abs(DirectionUtils.Difference(result.Chair.RadianDirection, facingDir)) > Math.PI / 4))
                    return; //not a valid goal.
                if (result.Chair == null && OnlySit) return;

                score = score * ((result.Chair != null) ? Slot.Sitting : Slot.Standing);
                //if an avatar is in or going to our destination positon, we this spot becomes low priority as getting into it will require a shoo.
                if (!avatarInWay)
                {
                    foreach (var avatar in context.ObjectQueries.Avatars)
                    {
                        if (avatar == caller) continue;
                        //search for routing frame. is its destination the same as ours?
                        if (avatar.Thread != null)
                        {
                            var intersects = avatar.Thread.Stack.Any(x => x is VMRoutingFrame && ((VMRoutingFrame)x).IntersectsOurDestination(result));
                            if (intersects)
                            {
                                score = Math.Max(double.Epsilon, score);
                                break;
                            }
                        }
                    }
                }
                else score = Math.Max(double.Epsilon, score);
            }
            result.Score = score;

            Results.Add(result);
        }

        private void SetFail(VMRouteFailCode code, VMEntity blocker)
        {
            if (Array.IndexOf(FailPrio, code) > Array.IndexOf(FailPrio, FailCode))
            {
                FailCode = code;
                Blocker = blocker;
            }
        }

        private static double FlagsAsRad(SLOTFlags dir)
        {
            double value = Math.Round(Math.Log((int)dir & 255, 2))*Math.PI/4;
            //if (value > Math.PI) value -= 2*Math.PI;
            return value;
        }

        /// <summary>
        /// Returns which search direction (n/s/e/w/ne/nw/se/sw) a target is in relation to an object. (absolute)
        /// </summary>
        /// <param name="pos1">The position of the source.</param>
        /// <param name="pos2">The position of the target.</param>
        /// <returns></returns>
        public static SLOTFlags GetSearchDirection(Vector2 pos1, Vector2 pos2, float rotShift){

            double dir = Math.Atan2(Math.Floor(pos2.X) - Math.Floor(pos1.X), Math.Floor(pos1.Y) - Math.Floor(pos2.Y)) * (180.0/Math.PI);
            dir = DirectionUtils.NormalizeDegrees(dir - rotShift * (180.0 / Math.PI));
            SLOTFlags result = (SLOTFlags)0;

            if (dir >= -45.0 - ANGLE_ERROR && dir <= 45.0 + ANGLE_ERROR) result |= SLOTFlags.NORTH;
            if (dir >= 0.0 - ANGLE_ERROR && dir <= 90.0 + ANGLE_ERROR) result |= SLOTFlags.NORTH_EAST;
            if (dir >= 45.0 - ANGLE_ERROR && dir <= 135.0 + ANGLE_ERROR) result |= SLOTFlags.EAST;
            if ((dir >= 90.0 - ANGLE_ERROR && dir <= 180.0 + ANGLE_ERROR) || (dir <= -180.0 + ANGLE_ERROR)) result |= SLOTFlags.SOUTH_EAST;
            if (dir >= 135.0 - ANGLE_ERROR || dir <= -135.0 + ANGLE_ERROR) result |= SLOTFlags.SOUTH;
            if ((dir >= -180.0 - ANGLE_ERROR && dir <= -90.0 + ANGLE_ERROR) || (dir >= 180.0 - ANGLE_ERROR)) result |= SLOTFlags.SOUTH_WEST;
            if (dir >= -135.0 - ANGLE_ERROR && dir <= -45.0 + ANGLE_ERROR) result |= SLOTFlags.WEST;
            if (dir >= -90.0 - ANGLE_ERROR && dir <= 0.0 + ANGLE_ERROR) result |= SLOTFlags.NORTH_WEST; 

            return result;
        }

        public static SLOTFlags RadianToFlags(double rad)
        {
            int result = (int)(Math.Round((rad / (Math.PI * 2)) * 8) + 80) % 8; //for best results, make sure rad is >-pi and <pi
            return (SLOTFlags)(1 << result);
        }

        public static double GetDirectionTo(Vector2 pos1, Vector2 pos2)
        {
            return Math.Atan2(pos2.X - pos1.X, -(pos2.Y - pos1.Y));
        }
    }

    public class VMFindLocationResult
    {
        public float RadianDirection;
        public LotTilePos Position;
        public double Score;
        public bool FaceAnywhere = false;
        public VMEntity Chair;
        public SLOTFlags RouteEntryFlags = SLOTFlags.NORTH;

        public VMFindLocationResult() { }

        #region VM Marshalling Functions
        public VMFindLocationResultMarshal Save()
        {
            return new VMFindLocationResultMarshal
            {
                RadianDirection = RadianDirection,
                Position = Position,
                Score = Score,
                FaceAnywhere = FaceAnywhere,
                Chair = (Chair == null) ? (short)0 : Chair.ObjectID,
                RouteEntryFlags = RouteEntryFlags
            };
        }

        public void Load(VMFindLocationResultMarshal input, VMContext context)
        {
            RadianDirection = input.RadianDirection;
            Position = input.Position;
            Score = input.Score;
            FaceAnywhere = input.FaceAnywhere;
            Chair = context.VM.GetObjectById(input.Chair);
            RouteEntryFlags = input.RouteEntryFlags;
        }

        public VMFindLocationResult(VMFindLocationResultMarshal input, VMContext context)
        {
            Load(input, context);
        }
        #endregion
    }
}
