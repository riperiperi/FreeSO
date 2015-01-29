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


            Vector2 center = new Vector2(obj.Position.X, obj.Position.Y);
            if (!(obj is VMAvatar)) center += new Vector2(0.5f, 0.5f);

            var rotOff = Vector3.Transform(slot.Offset, Matrix.CreateRotationZ(ObjectRotAsRad(obj.Direction)));
            //center += new Vector2(rotOff.X/16, rotOff.Y/16);
            var circleCtr = new Vector2(center.X + rotOff.X / 16, center.Y + rotOff.Y / 16);

            SLOTFlags flags = slot.Rsflags;
            //if (slot.Facing == -3) flags |= SLOTFlags.FacingAwayFromObject;

            int minProximity = slot.MinProximity;
            int maxProximity = slot.MaxProximity;
            int desiredProximity = slot.OptimalProximity;

            if (maxProximity == 0) { maxProximity = minProximity; }
            if (desiredProximity == 0) { desiredProximity = minProximity; }

            var result = new List<VMFindLocationResult>();

            if (((int)flags & 255) == 0)
            {
                minProximity = 0;
                maxProximity = 0;
                desiredProximity = 0;
                flags |= (SLOTFlags)255;
            }

            //rotate search direction flags to match object direction if slot flags are not "absolute".
            flags = RotateByObjectDir(flags, obj.Direction, false);

            if ((flags & SLOTFlags.SnapToDirection) > 0)
            { //do not change location, instead snap to the specified direction.
                result.Add(new VMFindLocationResult
                {
                    Flags = flags,
                    Position = new Vector3(center.X, center.Y, 0), //force ground floor for now
                    Proximity = 0
                });
            }

            var totalBox = 0;

            for (int x=-maxProximity; x<=maxProximity; x+=16) {
                for (int y = -maxProximity; y <= maxProximity; y += 16)
                {
                    var pos = new Vector2(circleCtr.X + x / 16.0f, circleCtr.Y + y / 16.0f);
                    double distance = Math.Sqrt(x*x+y*y);
                    if (distance >= minProximity - 0.01 && distance <= maxProximity + 0.01) //slot is within proximity
                    {
                        if (!context.SolidToAvatars(new VMTilePos((short)(pos.X), (short)(pos.Y), 1))) //not occupied, or going to be (soon)
                        {
                            if ((GetSearchDirection(center, pos) & flags) > 0) //within search location
                            {
                                SLOTFlags facingDir;
                                
                                switch (slot.Facing) {
                                    case SLOTFacing.FaceTowardsObject:
                                        facingDir = GetDirectionTo(pos, center); break;
                                    case SLOTFacing.FaceAwayFromObject:
                                        facingDir = GetDirectionTo(center, pos); break;
                                    default:
                                        facingDir = SLOTFlags.NORTH; break;
                                }
                                result.Add(new VMFindLocationResult
                                {
                                    Flags = facingDir | (flags & unchecked((SLOTFlags)0xFFFFFF00)),
                                    Position = new Vector3(pos.X, pos.Y, 0), //force ground floor for now
                                    Proximity = 0
                                });
                            }
                        }
                    }
                }
            }

            /** Sort by how close they are to desired proximity **/
            //result.Sort(new VMProximitySorter(desiredProximity));
            

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
        public static SLOTFlags GetSearchDirection(Vector2 pos1, Vector2 pos2){
            //target.X -= center.X;
            //target.Y -= center.Y;

            double dir = Math.Atan2(Math.Floor(pos2.X) - Math.Floor(pos1.X), Math.Floor(pos1.Y) - Math.Floor(pos2.Y)) * (180.0/Math.PI);
            SLOTFlags result = (SLOTFlags)0;

            if (dir >= -45.0 && dir <= 45.0) result |= SLOTFlags.NORTH;
            if (dir >= 0.0 && dir <= 90.0) result |= SLOTFlags.NORTH_EAST;
            if (dir >= 45.0 && dir <= 135.0) result |= SLOTFlags.EAST;
            if (dir >= 90.0 && dir <= 180.0) result |= SLOTFlags.SOUTH_EAST;
            if (dir >= 135.0 || dir <= -135.0) result |= SLOTFlags.SOUTH;
            if (dir >= -180.0 && dir <= -135.0) result |= SLOTFlags.SOUTH_WEST;
            if (dir >= -135.0 && dir <= -45.0) result |= SLOTFlags.WEST;
            if (dir >= -90.0 && dir <= 0.0) result |= SLOTFlags.NORTH_WEST; 

            return result;
        }

        public static SLOTFlags GetDirectionTo(Vector2 pos1, Vector2 pos2)
        {
            int result = (int)(Math.Round((Math.Atan2(Math.Floor(pos2.X) - Math.Floor(pos1.X), Math.Floor(pos2.Y) - Math.Floor(pos1.Y)) / (Math.PI * 2)) * 8) + 24) % 8;
            return (SLOTFlags)(1 << result);
        }
    }

    public class VMFindLocationResult
    {
        public SLOTFlags Flags;
        public Vector3 Position;
        public int Proximity;
    }

    public class VMProximitySorter : IComparer<VMFindLocationResult>
    {
        private int DesiredProximity;

        public VMProximitySorter(int desiredProximity){
            this.DesiredProximity = desiredProximity;
        }


        #region IComparer<VMFindLocationResult> Members

        public int Compare(VMFindLocationResult x, VMFindLocationResult y)
        {
            var distanceX = Math.Abs(x.Proximity - DesiredProximity);
            var distanceY = Math.Abs(y.Proximity - DesiredProximity);

            if (distanceX < distanceY){
                return -1;
            }else if (distanceX > distanceY)
            {
                return -1;
            }else
            {
                return ((int)x.Flags).CompareTo((int)y.Flags);
            }
        }

        #endregion
    }
}
