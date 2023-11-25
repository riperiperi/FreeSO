using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace FSO.SimAntics.Model.Routing
{
    public class VMWalkableRect : VMObstacle
    {
        public VMFreeList[] Free;
        public List<VMWalkableRect> Adj;

        //used directly for routing
        public VMWalkableRect Parent;
        public Point ParentSource;
        public Point ParentSourceHiP;
        public int OriginalG;
        public int FScore;
        public int GScore;
        public bool Start;
        public byte State; //0 = untouched, 1 = open, 2 = closed;

        //assigned by optimizer when the line in this rect has been optimized over
        //bezier generation uses this to place lines between multiple rects without heavy subdivision
        public int LineID = -1;

        public VMWalkableRect(int x1, int y1, int x2, int y2) : base(x1,y1,x2,y2)
        {
            Free = new VMFreeList[4];
            Adj = new List<VMWalkableRect>();
        }

        public Point ClosestOnSharedEdge(VMWalkableRect other, Point pt)
        {
            if (x2 == other.x1) //right side
                return new Point(x2, Math.Min(Math.Min(y2, other.y2), Math.Max(Math.Max(y1, other.y1), pt.Y)));
            else if (x1 == other.x2) //left side
                return new Point(x1, Math.Min(Math.Min(y2, other.y2), Math.Max(Math.Max(y1, other.y1), pt.Y)));
            else if (y1 == other.y2) //top side
                return new Point(Math.Min(Math.Min(other.x2, x2), Math.Max(Math.Max(x1, other.x1), pt.X)), y1);
            else if (y2 == other.y1) //bottom side
                return new Point(Math.Min(Math.Min(other.x2, x2), Math.Max(Math.Max(x1, other.x1), pt.X)), y2);

            return pt; //not in contact...
        }

        public Point ClosestOnHiSharedEdge(VMWalkableRect other, Point pt)
        {
            var hiMul = 0x8000;
            if (x2 == other.x1) //right side
                return new Point(x2*hiMul, Math.Min(Math.Min(y2, other.y2) * hiMul, Math.Max(Math.Max(y1, other.y1) * hiMul, pt.Y)));
            else if (x1 == other.x2) //left side
                return new Point(x1 * hiMul, Math.Min(Math.Min(y2, other.y2) * hiMul, Math.Max(Math.Max(y1, other.y1) * hiMul, pt.Y)));
            else if (y1 == other.y2) //top side
                return new Point(Math.Min(Math.Min(other.x2, x2) * hiMul, Math.Max(Math.Max(x1, other.x1) * hiMul, pt.X)), y1 * hiMul);
            else if (y2 == other.y1) //bottom side
                return new Point(Math.Min(Math.Min(other.x2, x2) * hiMul, Math.Max(Math.Max(x1, other.x1) * hiMul, pt.X)), y2 * hiMul);

            return pt; //not in contact...
        }
    }
}
