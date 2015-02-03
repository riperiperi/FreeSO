using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using TSO.Files.formats.iff.chunks;
using tso.world.model;

namespace TSO.Simantics.engine
{
    public class VMSlotParser
    {
        public const double ANGLE_ERROR = 0.01f;

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
            SLOTFlags flags = slot.Rsflags;
            Vector2 center;

            if (((flags & SLOTFlags.UseAverageObjectLocation) > 0) && (obj.MultitileGroup != null)) {
                center = new Vector2(0, 0);
                var objs = obj.MultitileGroup.Objects;
                for (int i = 0; i < objs.Count; i++)
                {
                    center += new Vector2(objs[i].Position.X, objs[i].Position.Y);
                }
                center /= objs.Count;
            } else center = new Vector2(obj.Position.X, obj.Position.Y);
            if (!(obj is VMAvatar)) center += new Vector2(0.5f, 0.5f);

            var rotOff = Vector3.Transform(slot.Offset, Matrix.CreateRotationZ(obj.RadianDirection));
            //center += new Vector2(rotOff.X/16, rotOff.Y/16);
            var circleCtr = new Vector2(center.X + rotOff.X / 16, center.Y + rotOff.Y / 16);

            //if (slot.Facing == -3) flags |= SLOTFlags.FacingAwayFromObject;

            int minProximity = slot.MinProximity;
            int maxProximity = slot.MaxProximity;
            int desiredProximity = slot.OptimalProximity;

            if (maxProximity == 0) { maxProximity = minProximity; }
            if (desiredProximity == 0) { desiredProximity = minProximity; }

            var result = new List<VMFindLocationResult>();

            //rotate search direction flags to match object direction if slot flags are not "absolute".
            //flags = RotateByObjectDir(flags, obj.Direction, false);

            if ((flags & SLOTFlags.SnapToDirection) > 0)
            { //do not change location, instead snap to the specified direction.
                if (((int)flags & 255) == 0) flags |= SLOTFlags.NORTH;
                var fdflags = RotateByObjectDir(flags, obj.Direction, false);

                result.Add(new VMFindLocationResult
                {
                    Flags = fdflags,
                    Position = new Vector3(circleCtr.X, circleCtr.Y, 0), //force ground floor for now
                    Score = 0
                });
                return result;
            }
            else
            {
                if (((int)flags & 255) == 0)
                {
                    minProximity = 0;
                    maxProximity = 0;
                    desiredProximity = 0;
                    flags |= (SLOTFlags)255;
                }
                var maxScore = Math.Max(desiredProximity - minProximity, maxProximity - desiredProximity);

                for (int x = -maxProximity; x <= maxProximity; x += 16)
                {
                    for (int y = -maxProximity; y <= maxProximity; y += 16)
                    {
                        var pos = new Vector2(circleCtr.X + x / 16.0f, circleCtr.Y + y / 16.0f);
                        double distance = Math.Sqrt(x * x + y * y);
                        if (distance >= minProximity - 0.01 && distance <= maxProximity + 0.01) //slot is within proximity
                        {
                            //todo: get routing modes (standing/sitting/on floor?/none)
                            var solidRes = context.SolidToAvatars(new VMTilePos((short)(pos.X), (short)(pos.Y), 1));
                            if ((!solidRes.Solid) || (slot.Sitting > 0 && solidRes.Chair != null)) //not occupied, or going to be (soon)
                            {
                                var routeEntryFlags = (GetSearchDirection(center, pos, obj.RadianDirection) & flags);
                                if (routeEntryFlags > 0) //within search location
                                {

                                    //var testo = context.VM.Context.CreateObjectInstance(0x00000437, (short)pos.X, (short)pos.Y, 1, Direction.NORTH);
                                    //testo.Init(context.VM.Context);

                                    float facingDir;

                                    switch (slot.Facing)
                                    {
                                        case SLOTFacing.FaceTowardsObject:
                                            facingDir = (float)GetDirectionTo(pos, center); break;
                                        case SLOTFacing.FaceAwayFromObject:
                                            facingDir = (float)GetDirectionTo(center, pos); break;
                                        case SLOTFacing.FaceAnywhere:
                                            facingDir = 0.0f; break;
                                        default:
                                            int intDir = (int)Math.Round(Math.Log((double)obj.Direction, 2));
                                            var rotatedF = ((int)slot.Facing + intDir) % 8;
                                            facingDir = (float)(((int)rotatedF > 4) ? ((double)rotatedF * Math.PI / 4.0) : (((double)rotatedF - 8.0) * Math.PI / 4.0)); break;
                                    }

                                    var fdflags = RadianToFlags(facingDir);

                                    if (solidRes.Chair != null && fdflags != (SLOTFlags)solidRes.Chair.Direction) continue;

                                    result.Add(new VMFindLocationResult
                                    {
                                        Flags = fdflags | (flags & unchecked((SLOTFlags)0xFFFFFF00)),
                                        Position = new Vector3(pos.X, pos.Y, 0), //force ground floor for now
                                        Score = ((maxScore - Math.Abs(desiredProximity - distance)) + context.VM.Context.NextRandom(1024) / 1024.0f) * ((solidRes.Chair != null) ? slot.Sitting : slot.Standing), //todo: prefer closer?
                                        RadianDirection = facingDir,
                                        Chair = solidRes.Chair,
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

        private static float ObjectRotAsRad(Direction dir)
        {
            switch (dir)
            {
                case Direction.NORTH:
                    return 0;
                case Direction.EAST:
                    return (float)Math.PI / 2;
                case Direction.SOUTH:
                    return (float)Math.PI;
                case Direction.WEST:
                    return (float)Math.PI * 1.5f;
                default:
                    return 0;
            }
        }

        public static SLOTFlags RotateByObjectDir(SLOTFlags input, Direction dir, bool negative)
        {
            int rotBits = 0;
            switch (dir)
            {
                case Direction.NORTH:
                    rotBits = 0;
                    break;
                case Direction.EAST:
                    rotBits = 2;
                    break;
                case Direction.SOUTH:
                    rotBits = 4;
                    break;
                case Direction.WEST:
                    rotBits = 6;
                    break;
            }
            if (negative) rotBits = (8 - rotBits) % 8;
            int flagRot = ((int)input & 255) << rotBits;
            return (SLOTFlags)((flagRot & 255) | (flagRot >> 8) | ((int)input&unchecked((int)0xFFFFFF00)));
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
            var rot2 = rotShift * (180.0 / Math.PI);
            dir = PosMod(dir - rot2, 360);
            if (dir > 180) dir -= 360;
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

        private static double PosMod(double x, double m)
        {
            return (x % m + m) % m;
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
        public Vector3 Position;
        public double Score;
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
