/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;

namespace tso.world.model
{
    /// <summary>
    /// Generates and manages a room map for a specified level.
    /// </summary>
    public class RoomMap
    {
        public ushort[] Map;
        public int Width;
        public int Height;

        /// <summary>
        /// Generates the room map for the specified walls array. Returns the next free room to be assigned.
        /// </summary>
        public ushort GenerateMap(WallTile[] Walls, int width, int height, ushort curRoom) //for first floor gen, curRoom should be 1. For floors above, it should be the last genmap result
        {
            Map = new ushort[width*height]; //although 0 is the base of the array, room 1 is known to simantics as room 0.
            //values of 0 indicate the room has not been chosen in that location yet.

            this.Width = width;
            this.Height = height;

            //flood fill recursively. Each time choose find and choose the first "0" as the base.
            //The first recursion (outside) cannot fill into diagonals.
            bool remaining = true;
            while (remaining)
            {
                var spread = new Stack<Point>();
                remaining = false;
                for (int i = 0; i < Map.Length; i++)
                {
                    if (Map[i] == 0)
                    {
                        remaining = true;
                        Map[i] = curRoom;
                        spread.Push(new Point(i % width, i / width));
                        break;
                    }
                }

                if (remaining)
                {
                    while (spread.Count > 0)
                    {
                        var item = spread.Pop();

                        var plusX = (item.X+1)%width;
                        var minX = (item.X + width - 1) % width;
                        var plusY = (item.Y+1)%height;
                        var minY = (item.Y + height - 1) % height;

                        var mainWalls = Walls[item.X + item.Y * width];
                        if ((byte)mainWalls.Segments > 15) continue; //don't spread on diagonals for now

                        var PXWalls = Walls[plusX + item.Y * width];
                        var PYWalls = Walls[item.X + plusY * width];

                        if (Map[plusX + item.Y * width] == 0 && ((PXWalls.Segments & WallSegments.TopLeft) == 0 || PXWalls.TopLeftStyle != 1)) 
                            { Map[plusX + item.Y * width] = curRoom; spread.Push(new Point(plusX, item.Y)); }
                        if (Map[minX + item.Y * width] == 0 && ((mainWalls.Segments & WallSegments.TopLeft) == 0 || mainWalls.TopLeftStyle != 1)) 
                            { Map[minX + item.Y * width] = curRoom; spread.Push(new Point(minX, item.Y)); }
                        if (Map[item.X + plusY * width] == 0 && ((PYWalls.Segments & WallSegments.TopRight) == 0 || PYWalls.TopRightStyle != 1))
                            { Map[item.X + plusY * width] = curRoom; spread.Push(new Point(item.X, plusY)); }
                        if (Map[item.X + minY * width] == 0 && ((mainWalls.Segments & WallSegments.TopRight) == 0 || mainWalls.TopRightStyle != 1))
                            { Map[item.X + minY * width] = curRoom; spread.Push(new Point(item.X, minY)); }
                    }
                    curRoom++;
                }
            }
            return curRoom;
        }

        public void PrintRoomMap()
        {
            int off = 0;
            for (int y = 0; y < Height; y++)
            {
                StringBuilder sb = new StringBuilder();
                for (int x = 0; x < Width; x++)
                {
                    sb.Append(Map[off++]);
                    if (Map[off - 1] < 10) sb.Append(" ");
                    sb.Append(" ");
                }
                System.Diagnostics.Debug.WriteLine(sb.ToString());
            }
        }
    }
}
