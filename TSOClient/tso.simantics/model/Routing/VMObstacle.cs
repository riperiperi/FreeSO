/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Model.Routing
{
    public class VMObstacle //like an XNA rectangle but a little different for our purposes
    {
        public int x1;
        public int x2;
        public int y1;
        public int y2;

        public VMObstacle(int x1, int y1, int x2, int y2)
        {
            if (x1 > x2) { var temp = x1; x1 = x2; x2 = temp; }
            if (y1 > y2) { var temp = y1; y1 = y2; y2 = temp; }

            this.x1 = x1;
            this.y1 = y1;
            this.x2 = x2;
            this.y2 = y2;
        }

        public VMObstacle(Point source, Point dest) : this(source.X, source.Y, dest.X, dest.Y) { 
        }

        public bool Contains(Point pt)
        {
            return (pt.X >= x1 && pt.X <= x2) && (pt.Y >= y1 && pt.Y <= y2);
        }

        public bool ContainsHiP(Point pt)
        {
            return (pt.X >= x1 * 0x8000 && pt.X <= x2 * 0x8000) && (pt.Y >= y1 * 0x8000 && pt.Y <= y2 * 0x8000);
        }

        public bool HardContains(Point pt)
        {
            return (pt.X > x1 && pt.X < x2) && (pt.Y > y1 && pt.Y < y2);
        }

        public Point Closest(int x, int y)
        {
            return new Point(Math.Max(Math.Min(x2, x), x1), Math.Max(Math.Min(y2, y), y1));
        }


        public Point ClosestHiP(int x, int y)
        {
            return new Point(Math.Max(Math.Min(x2 * 0x8000, x), x1 * 0x8000), Math.Max(Math.Min(y2 * 0x8000, y), y1 * 0x8000));
        }

        public bool Intersects(VMObstacle other)
        {
            return !((other.x1 >= x2 || other.x2 <= x1) || (other.y1 >= y2 || other.y2 <= y1));
        }

        public Rectangle ToRectangle()
        {
            return new Rectangle(x1, y1, x2 - x1, y2 - y1);
        }

        public Point? RaycastLine(Point from, Point to)
        {
            var diff = to - from;

            //furthest exit point is most important
            var best = int.MaxValue;
            Point? leaves = null;

            int xOut;
            if (diff.X > 0) xOut = x2; //moving right, leaves through right
            else xOut = x1; //moving left, leaves through left

            //at what point does this line's x pass xOut?
            var xDiff = xOut - from.X;
            if (diff.X != 0)
            {
                int t = (xDiff * 32768) / diff.X;
                int yHit = from.Y + ((t * diff.Y) / 32768);
                if (yHit >= y1 && yHit <= y2)
                {
                    //bingo - we hit the x leaving line.
                    best = t;
                    leaves = new Point(xOut, yHit);
                }
            }

            int yOut;
            if (diff.Y > 0) yOut = y2; //moving down, leaves through bottom
            else yOut = y1; //moving up, leaves through top

            //at what point does this line's y pass yOut?
            var yDiff = yOut - from.Y;
            if (diff.Y != 0)
            {
                int t = (yDiff * 32768) / diff.Y;
                int xHit = from.X + ((t * diff.X) / 32768);
                if (xHit >= x1 && xHit <= x2)
                {
                    //bingo - we hit the y leaving line.
                    if (t < best)
                    {
                        best = t;
                        leaves = new Point(xHit, yOut);
                    }
                }
            }

            return leaves;
        }

        public Point? RaycastLineHiP(Point from, Point to)
        {
            var diff = to - from;

            //furthest exit point is most important
            var best = int.MaxValue;
            Point? leaves = null;

            int xOut;
            if (diff.X > 0) xOut = x2; //moving right, leaves through right
            else xOut = x1; //moving left, leaves through left
            xOut *= 0x8000;

            //at what point does this line's x pass xOut?
            var xDiff = xOut - from.X;
            if (diff.X != 0)
            {
                int t = (int)(((long)xDiff*0x8000) / diff.X);
                int yHit = from.Y + (int)((t * (long)diff.Y) / 0x8000);
                if (yHit >= y1 * 0x8000 && yHit <= y2 * 0x8000)
                {
                    //bingo - we hit the x leaving line.
                    best = t;
                    leaves = new Point(xOut, yHit);
                }
            }

            int yOut;
            if (diff.Y > 0) yOut = y2; //moving down, leaves through bottom
            else yOut = y1; //moving up, leaves through top
            yOut *= 0x8000;

            //at what point does this line's y pass yOut?
            var yDiff = yOut - from.Y;
            if (diff.Y != 0)
            {
                int t = (int)(((long)yDiff * 0x8000) / diff.Y);
                int xHit = from.X + (int)((t * (long)diff.X) / 0x8000);
                if (xHit >= x1 * 0x8000 && xHit <= x2 * 0x8000)
                {
                    //bingo - we hit the y leaving line.
                    if (t < best)
                    {
                        best = t;
                        leaves = new Point(xHit, yOut);
                    }
                }
            }

            return leaves;
        }
    }
}
