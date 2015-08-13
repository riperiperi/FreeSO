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
using FSO.LotView.Model;
using FSO.SimAntics.Model.Routing;

namespace FSO.SimAntics.Model
{
    /// <summary>
    /// Generates and manages a room map for a specified level.
    /// </summary>
    public class VMRoomMap
    {
        public ushort[] Map;
        public int Width;
        public int Height;

        /// <summary>
        /// Generates the room map for the specified walls array.
        /// </summary>
        public void GenerateMap(WallTile[] Walls, FloorTile[] Floors, int width, int height, List<LotRoom> rooms) //for first floor gen, curRoom should be 1. For floors above, it should be the last genmap result
        {
            Map = new ushort[width*height]; //although 0 is the base of the array, room 1 is known to simantics as room 0.
            //values of 0 indicate the room has not been chosen in that location yet.

            bool noFloorBad = (rooms.Count > 1);

            this.Width = width;
            this.Height = height;

            //flood fill recursively. Each time choose find and choose the first "0" as the base.
            //The first recursion (outside) cannot fill into diagonals.
            bool remaining = true;
            bool outside = true;
            while (remaining)
            {
                var spread = new Stack<Point>();
                remaining = false;
                for (int i = 0; i < Map.Length; i++)
                {
                    if (Map[i] == 0)
                    {
                        remaining = true;
                        Map[i] = (ushort)rooms.Count;
                        spread.Push(new Point(i % width, i / width));
                        break;
                    }
                }

                if (remaining)
                {
                    int rminX = spread.Peek().X;
                    int rmaxX = rminX;
                    int rminY = spread.Peek().Y;
                    int rmaxY = rminY;
                    var wallObs = new List<VMObstacle>();
                    while (spread.Count > 0)
                    {
                        var item = spread.Pop();

                        if (item.X > rmaxX) rmaxX = item.X;
                        if (item.X < rminX) rminX = item.X;
                        if (item.Y > rmaxY) rmaxY = item.Y;
                        if (item.Y < rminY) rminY = item.Y;

                        var plusX = (item.X+1)%width;
                        var minX = (item.X + width - 1) % width;
                        var plusY = (item.Y+1)%height;
                        var minY = (item.Y + height - 1) % height;

                        var mainWalls = Walls[item.X + item.Y * width];

                        int obsX = item.X << 4;
                        int obsY = item.Y << 4;

                        if ((mainWalls.Segments & WallSegments.HorizontalDiag) > 0)
                        {
                            wallObs.Add(new VMObstacle(obsX + 9, obsY - 1, obsX + 17, obsY + 7));
                            wallObs.Add(new VMObstacle(obsX + 4, obsY + 4, obsX + 12, obsY + 12));
                            wallObs.Add(new VMObstacle(obsX - 1, obsY + 9, obsX + 7, obsY + 17));
                        }

                        if ((mainWalls.Segments & WallSegments.VerticalDiag) > 0)
                        {
                            wallObs.Add(new VMObstacle(obsX - 1, obsY - 1, obsX + 7, obsY + 7));
                            wallObs.Add(new VMObstacle(obsX + 4, obsY + 4, obsX + 12, obsY + 12));
                            wallObs.Add(new VMObstacle(obsX + 9, obsY + 9, obsX + 17, obsY + 17));
                        }

                        if ((byte)mainWalls.Segments > 15) continue; //don't spread on diagonals for now

                        var PXWalls = Walls[plusX + item.Y * width];
                        var PYWalls = Walls[item.X + plusY * width];

                        if ((mainWalls.Segments & WallSegments.TopLeft) > 0 && !mainWalls.TopLeftDoor) wallObs.Add(new VMObstacle(obsX - 3, obsY - 3, obsX + 6, obsY + 19));
                        if ((mainWalls.Segments & WallSegments.TopRight) > 0 && !mainWalls.TopRightDoor) wallObs.Add(new VMObstacle(obsX - 3, obsY - 3, obsX + 19, obsY + 6)); 
                        if ((mainWalls.Segments & WallSegments.BottomLeft) > 0 && !PYWalls.TopRightDoor) wallObs.Add(new VMObstacle(obsX - 3, obsY + 13, obsX + 19, obsY + 19)); 
                        if ((mainWalls.Segments & WallSegments.BottomRight) > 0 && !PXWalls.TopLeftDoor) wallObs.Add(new VMObstacle(obsX + 13, obsY - 3, obsX + 19, obsY + 19));

                        if (Map[plusX + item.Y * width] == 0 && ((PXWalls.Segments & WallSegments.TopLeft) == 0 || PXWalls.TopLeftStyle != 1)) 
                            { Map[plusX + item.Y * width] = (ushort)rooms.Count; spread.Push(new Point(plusX, item.Y)); }
                        if (Map[minX + item.Y * width] == 0 && ((mainWalls.Segments & WallSegments.TopLeft) == 0 || mainWalls.TopLeftStyle != 1)) 
                            { Map[minX + item.Y * width] = (ushort)rooms.Count; spread.Push(new Point(minX, item.Y)); }
                        if (Map[item.X + plusY * width] == 0 && ((PYWalls.Segments & WallSegments.TopRight) == 0 || PYWalls.TopRightStyle != 1))
                            { Map[item.X + plusY * width] = (ushort)rooms.Count; spread.Push(new Point(item.X, plusY)); }
                        if (Map[item.X + minY * width] == 0 && ((mainWalls.Segments & WallSegments.TopRight) == 0 || mainWalls.TopRightStyle != 1))
                            { Map[item.X + minY * width] = (ushort)rooms.Count; spread.Push(new Point(item.X, minY)); }
                    }
                    rooms.Add(new LotRoom
                    {
                        IsOutside = outside,
                        Bounds = new Rectangle(rminX, rminY, (rmaxX - rminX) + 1, (rmaxY - rminY) + 1),
                        WallObs = wallObs
                    });
                    outside = false;
                }
            }
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
