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
            if (obj is VMAvatar) center -= new Vector2(0.5f, 0.5f);

            var rotOff = Vector3.Transform(slot.Offset, Matrix.CreateRotationZ(ObjectRotAsRad(obj.Direction)));
            center += new Vector2(rotOff.X/16, rotOff.Y/16);

            SLOTFlags flags = slot.Rsflags;
            if (slot.Facing == -3) flags |= SLOTFlags.FacingAwayFromObject;

            int minProximity = slot.MinProximity;
            int maxProximity = slot.MaxProximity;
            int desiredProximity = slot.OptimalProximity;

            if (maxProximity == 0) { maxProximity = minProximity; }
            if (desiredProximity == 0) { desiredProximity = minProximity; }

            var result = new List<VMFindLocationResult>();

            if (flags == 0)
            {
                flags |= SLOTFlags.SOUTH; //if flags are not set, location is literally in the exact position (no proximity)
                flags = RotateByObjectDir(flags, obj.Direction, false);
                result.Add(new VMFindLocationResult
                {
                    Flags = flags,
                    Position = new Vector3(center.X + 0.5f, center.Y + 0.5f, 0), //force ground floor for now
                    Proximity = 0
                });
                return result;
            }

            var proximity = minProximity;
            var proximityIncrement = 16;
            var proximityNudge = proximityIncrement * 0.25f;
            var currentDepth = 1.0f;

            //rotate directional facing flags if slot flags are not "absolute".
            flags = RotateByObjectDir(flags, obj.Direction, false);

            while (proximity <= maxProximity){
                var angle = 0.0f;
                /** Every time we move out by 1 tile in proximity, there will be more tiles to look at **/
                var angleIncrement = 360.0f / (currentDepth * 8);

                while (angle < 360.0f){
                    var radians = angle * (Math.PI / 180.0f);
                    var radius = proximity + proximityNudge;

                    var xpos = Math.Round(radius * Math.Cos(radians));
                    var ypos = Math.Round(radius * Math.Sin(radians));

                    var tileX = (float)Math.Round(xpos / 16.0f) + center.X;
                    var tileY = (float)Math.Round(ypos / 16.0f) + center.Y;

                    if (!context.SolidToAvatars(new VMTilePos((short)(tileX + 0.5f), (short)(tileY + 0.5f), 1)))
                    {

                        //we want to find slots where ANDing the rotated direction against a criteria (eg, facing towards, away) results in a value that is not 0.

                        SLOTFlags criteria = (SLOTFlags)255;
                        if ((flags & SLOTFlags.FacingAwayFromObject) == SLOTFlags.FacingAwayFromObject)
                        {
                            criteria = GetDirection(center, new Vector2(tileX, tileY));
                        }
                        else
                        {
                            criteria = GetDirection(new Vector2(tileX, tileY), center);
                        }

                        var temp = (flags & criteria);

                        if (temp > 0 || (flags & SLOTFlags.FaceAnywhere) == SLOTFlags.FaceAnywhere) //criteria met, add this location
                        {
                            result.Add(new VMFindLocationResult
                            {
                                Flags = temp | (flags & unchecked((SLOTFlags)0xFFFFFF00)),
                                Position = new Vector3(tileX + 0.5f, tileY + 0.5f, 0), //force ground floor for now
                                Proximity = proximity
                            });
                        }
                    }
                    angle += angleIncrement;
                }

                proximity += proximityIncrement;
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
        /// Returns which direction (n/s/e/w/ne/nw/se/sw) a target is in relation to an object.
        /// </summary>
        /// <param name="center"></param>
        /// <param name="target"></param>
        /// <returns></returns>
        public static SLOTFlags GetDirection(Vector2 center, Vector2 target){
            target.X -= center.X;
            target.Y -= center.Y;

            if (target.Y < 0){
                if (target.X > 0){
                    return SLOTFlags.NORTH_EAST;
                }else if (target.X < 0){
                    return SLOTFlags.NORTH_WEST;
                }
                return SLOTFlags.NORTH;
            }else if (target.Y > 0){
                if (target.X > 0){
                    return SLOTFlags.SOUTH_EAST;
                }else if (target.Y < 0){
                    return SLOTFlags.SOUTH_WEST;
                }
                return SLOTFlags.SOUTH;
            }

            if (target.X < 0) { return SLOTFlags.WEST; }
            return SLOTFlags.EAST;
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
