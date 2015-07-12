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
using TSO.Files.formats.iff.chunks;
using tso.world.model;
using TSO.Common.utils;

namespace TSO.Simantics.engine
{
    public class VMSlotParser
    {
        public const double ANGLE_ERROR = 0.01f;
        // TODO: float values may desync if devices are not both x86 or using a different C# library. 
        // Might need to replace with fixed point library for position and rotation

        /// <summary>
        /// This method will find all the avaliable locations within the criteria ordered by proximity to the optimal proximity
        /// External functions can then decide which is most desirable. E.g. the nearest slot to the object may be the longest route if
        /// its in another room.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="flags"></param>
        /// <param name="minProximity"></param>
        /// <param name="maxProximity"></param>
        /// <param name="desiredProximity"></param>
        /// <returns></returns>
        public static List<VMFindLocationResult> FindAvaliableLocations(VMEntity obj, SLOTItem slot, VMContext context)
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

            SLOTFlags flags = slot.Rsflags;
            Vector2 center;

            // if we need to use the average location of an object group, it needs to be calculated.
            if (((flags & SLOTFlags.UseAverageObjectLocation) > 0) && (obj.MultitileGroup.MultiTile)) {
                center = new Vector2(0, 0);
                var objs = obj.MultitileGroup.Objects;
                for (int i = 0; i < objs.Count; i++)
                {
                    center += new Vector2(objs[i].Position.x/16f, objs[i].Position.y/16f);
                }
                center /= objs.Count;
            } else center = new Vector2(obj.Position.x/16f, obj.Position.y/16f);

            //add offset of slot if it exists. must be rotated to be relative to object
            var rotOff = Vector3.Transform(slot.Offset, Matrix.CreateRotationZ(obj.RadianDirection));
            var circleCtr = new Vector2(center.X + rotOff.X / 16, center.Y + rotOff.Y / 16);

            int minProximity = slot.MinProximity;
            int maxProximity = slot.MaxProximity;
            int desiredProximity = slot.OptimalProximity;
            ushort room = context.VM.Context.GetRoomAt(obj.Position);

            if (maxProximity == 0) { maxProximity = minProximity; }
            if (desiredProximity == 0) { desiredProximity = minProximity; }

            var result = new List<VMFindLocationResult>();

            if ((flags & SLOTFlags.SnapToDirection) > 0)
            { //do not change location, instead snap to the specified direction.
                if (((int)flags & 255) == 0) flags |= SLOTFlags.NORTH;

                var flagRot = DirectionUtils.PosMod(obj.RadianDirection+FlagsAsRad(flags), Math.PI*2);
                if (flagRot > Math.PI) flagRot -= Math.PI * 2;

                result.Add(new VMFindLocationResult
                {
                    Flags = flags,
                    Position = new LotTilePos((short)Math.Round(circleCtr.X*16), (short)Math.Round(circleCtr.Y*16), 1), //force ground floor for now
                    RadianDirection = (float)flagRot,
                    FaceAnywhere = false,
                    Score = 0
                });
                return result;
            }
            else
            {
                if (((int)flags & 255) == 0)
                {
                    //exact position
                    minProximity = 0;
                    maxProximity = 0;
                    desiredProximity = 0;
                    flags |= (SLOTFlags)255;

                    // special case, walk directly to point. 
                    // double special case - facing direction seems to influence what ways the sim can enter this slot from, 
                    // but for now sims can walk through anything on the target point.

                    float facingDir;
                    bool faceAnywhere = false;

                    switch (slot.Facing)
                    {
                        case SLOTFacing.FaceTowardsObject:
                            facingDir = (float)GetDirectionTo(circleCtr, center); break;
                        case SLOTFacing.FaceAwayFromObject:
                            facingDir = (float)GetDirectionTo(center, circleCtr); break;
                        case SLOTFacing.FaceAnywhere:
                            faceAnywhere = true;
                            facingDir = 0.0f; break;
                        default:
                            int intDir = (int)Math.Round(Math.Log((double)obj.Direction, 2));
                            var rotatedF = ((int)slot.Facing + intDir) % 8;
                            facingDir = (float)(((int)rotatedF > 4) ? ((double)rotatedF * Math.PI / 4.0) : (((double)rotatedF - 8.0) * Math.PI / 4.0)); break;
                    }

                    result.Add(new VMFindLocationResult
                    {
                        Flags = flags,
                        Position = new LotTilePos((short)Math.Round(circleCtr.X * 16), (short)Math.Round(circleCtr.Y * 16), 1), //force ground floor for now
                        Score = 0,
                        RadianDirection = facingDir,
                        FaceAnywhere = faceAnywhere,
                    });

                    return result;
                }
                var maxScore = Math.Max(desiredProximity - minProximity, maxProximity - desiredProximity);
                var ignoreRooms = (flags & SLOTFlags.IgnoreRooms) > 0;

                for (int x = -maxProximity; x <= maxProximity; x += slot.Resolution)
                {
                    for (int y = -maxProximity; y <= maxProximity; y += slot.Resolution)
                    {
                        var pos = new Vector2(circleCtr.X + x / 16.0f, circleCtr.Y + y / 16.0f);
                        double distance = Math.Sqrt(x * x + y * y);
                        if (distance >= minProximity - 0.01 && distance <= maxProximity + 0.01 && (ignoreRooms || context.VM.Context.GetRoomAt(new LotTilePos((short)Math.Round(pos.X * 16), (short)Math.Round(pos.Y * 16), 1)) == room)) //slot is within proximity
                        {
                            var solidRes = context.SolidToAvatars(LotTilePos.FromBigTile((short)(pos.X), (short)(pos.Y), 1));
                            if ((!solidRes.Solid) || (slot.Sitting > 0 && solidRes.Chair != null)) //not occupied, or going to be (soon)
                            {
                                var routeEntryFlags = (GetSearchDirection(center, pos, obj.RadianDirection) & flags); //the route needs to know what conditions it fulfilled
                                if (routeEntryFlags > 0) //within search location
                                {
                                    //spawn placement squares at accepted positions
                                    //var testo = context.VM.Context.CreateObjectInstance(0x00000437, (short)pos.X, (short)pos.Y, 1, Direction.NORTH);
                                    //testo.Init(context.VM.Context);

                                    float facingDir;
                                    bool faceAnywhere = false;

                                    switch (slot.Facing)
                                    {
                                        case SLOTFacing.FaceTowardsObject:
                                            facingDir = (float)GetDirectionTo(pos, center); break;
                                        case SLOTFacing.FaceAwayFromObject:
                                            facingDir = (float)GetDirectionTo(center, pos); break;
                                        case SLOTFacing.FaceAnywhere:
                                            faceAnywhere = true;
                                            facingDir = 0.0f; break;
                                        default:
                                            int intDir = (int)Math.Round(Math.Log((double)obj.Direction, 2));
                                            var rotatedF = ((int)slot.Facing + intDir) % 8;
                                            facingDir = (float)(((int)rotatedF > 4) ? ((double)rotatedF * Math.PI / 4.0) : (((double)rotatedF - 8.0) * Math.PI / 4.0)); break;
                                    }

                                    if (solidRes.Chair != null)
                                    {
                                        if ((Math.Abs(DirectionUtils.Difference(solidRes.Chair.RadianDirection, facingDir)) > Math.PI / 4)) continue;
                                    }

                                    result.Add(new VMFindLocationResult
                                    {
                                        Flags = flags,
                                        Position = new LotTilePos((short)Math.Round(pos.X * 16), (short)Math.Round(pos.Y * 16), 1), //force ground floor for now
                                        Score = ((maxScore - Math.Abs(desiredProximity - distance)) + context.VM.Context.NextRandom(1024) / 1024.0f) * ((solidRes.Chair != null) ? slot.Sitting : slot.Standing), //todo: prefer closer?
                                        RadianDirection = facingDir,
                                        Chair = solidRes.Chair,
                                        FaceAnywhere = faceAnywhere,
                                        RouteEntryFlags = routeEntryFlags
                                    });
                                }
                            }
                        }
                    }
                }
            }
            /** Sort by how close they are to desired proximity **/
            if (result.Count > 1) result.Sort(new VMProximitySorter());
            

            return result;
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
            //target.X -= center.X;
            //target.Y -= center.Y;

            double dir = Math.Atan2(Math.Floor(pos2.X) - Math.Floor(pos1.X), Math.Floor(pos1.Y) - Math.Floor(pos2.Y)) * (180.0/Math.PI);
            dir = DirectionUtils.NormalizeDegrees(dir - rotShift * (180.0 / Math.PI));
            SLOTFlags result = (SLOTFlags)0;

            if (dir >= -45.0 - ANGLE_ERROR && dir <= 45.0 + ANGLE_ERROR) result |= SLOTFlags.NORTH;
            if (dir >= 0.0 - ANGLE_ERROR && dir <= 90.0 + ANGLE_ERROR) result |= SLOTFlags.NORTH_EAST;
            if (dir >= 45.0 - ANGLE_ERROR && dir <= 135.0 + ANGLE_ERROR) result |= SLOTFlags.EAST;
            if ((dir >= 90.0 - ANGLE_ERROR && dir <= 180.0 + ANGLE_ERROR) || (dir <= -180.0 + ANGLE_ERROR)) result |= SLOTFlags.SOUTH_EAST;
            if (dir >= 135.0 - ANGLE_ERROR || dir <= -135.0 + ANGLE_ERROR) result |= SLOTFlags.SOUTH;
            if ((dir >= -180.0 - ANGLE_ERROR && dir <= -135.0 + ANGLE_ERROR) || (dir >= 180.0 - ANGLE_ERROR)) result |= SLOTFlags.SOUTH_WEST;
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
            return Math.Atan2(Math.Floor(pos2.X) - Math.Floor(pos1.X), -(Math.Floor(pos2.Y) - Math.Floor(pos1.Y)));
        }
    }

    public class VMFindLocationResult
    {
        public SLOTFlags Flags;
        public float RadianDirection;
        public LotTilePos Position;
        public double Score;
        public bool FaceAnywhere = false;
        public VMEntity Chair;
        public SLOTFlags RouteEntryFlags = SLOTFlags.NORTH;
    }

    public class VMProximitySorter : IComparer<VMFindLocationResult>
    {

        #region IComparer<VMFindLocationResult> Members

        public int Compare(VMFindLocationResult x, VMFindLocationResult y)
        {
            if (x == null || y == null) return 0; //this happens occasionally. It's probably microsoft's fault, because the times it's happened nulls have never been in the array.
            //TODO: WARNING: this bug may cause clients to desync, if it's somehow caused by a race condition

            return (x.Score < y.Score)?1:-1;
        }

        #endregion
    }
}
