using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using TSO.Files.formats.iff.chunks;
using tso.world.model;

namespace TSO.Simantics.engine
{
    public class VMRouteFinder
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
        public static List<VMFindLocationResult> FindAvaliableLocations(VMEntity obj, SLOTItem slot)
        {
            /**
             * Start at min proximity and circle around the object to find the avaliable locations.
             * Then pick the one nearest to the optimal value
             */


            Vector2 center = new Vector2(obj.Position.X, obj.Position.Y);
            SLOTFlags flags = slot.Rsflags;
            int minProximity = slot.MinProximity;
            int maxProximity = slot.MaxProximity;
            int desiredProximity = slot.OptimalProximity;

            if (maxProximity == 0) { maxProximity = minProximity; }
            if (desiredProximity == 0) { desiredProximity = minProximity; }


            if (flags == 0) flags = SLOTFlags.FaceTowardsObject | SLOTFlags.NORTH; //if flags are not set, default to in front of, facing

            var result = new List<VMFindLocationResult>();
            
            var proximity = minProximity;
            var proximityIncrement = 16;
            var proximityNudge = proximityIncrement * 0.25f;
            var currentDepth = 1.0f;

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

                    var direction = GetDirection(center, new Vector2(tileX, tileY));
                    direction = RotateByObjectDir(direction, ((VMGameObject)obj).Direction, false); //todo, change to work for VMAvatar

                    if ((flags&direction) == direction)
                    {
                        //TODO: Check if slot is occupied or out of bounds

                        /** This is acceptible to the slot :) **/
                        result.Add(new VMFindLocationResult {
                            Direction = GetDirection(new Vector2(tileX, tileY), center),
                            Position = new Vector2(tileX, tileY),
                            Proximity = proximity
                        });
                    }
                    angle += angleIncrement;
                }

                proximity += proximityIncrement;
            }

            /** Sort by how close they are to desired proximity **/
            result.Sort(new VMProximitySorter(desiredProximity));
            

            return result;
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
            return (SLOTFlags)((flagRot & 255) | (flagRot >> 8));
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
        public SLOTFlags Direction;
        public Vector2 Position;
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
                return ((int)x.Direction).CompareTo((int)y.Direction);
            }
        }

        #endregion
    }
}
