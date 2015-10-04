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

        public bool Contains(Point pt)
        {
            return (pt.X >= x1 && pt.X <= x2) && (pt.Y >= y1 && pt.Y <= y2);
        }

        public bool HardContains(Point pt)
        {
            return (pt.X > x1 && pt.X < x2) && (pt.Y > y1 && pt.Y < y2);
        }

        public Point Closest(int x, int y)
        {
            return new Point(Math.Max(Math.Min(x2, x), x1), Math.Max(Math.Min(y2, y), y1));
        }

        public bool Intersects(VMObstacle other)
        {
            return !((other.x1 >= x2 || other.x2 <= x1) || (other.y1 >= y2 || other.y2 <= y1));
        }
    }
}
