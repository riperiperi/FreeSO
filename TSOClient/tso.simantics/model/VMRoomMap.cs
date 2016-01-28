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
        public uint[] Map;
        public int Width;
        public int Height;

        private ushort ExpectedTile;

        /// <summary>
        /// Generates the room map for the specified walls array.
        /// </summary>
        public void GenerateMap(WallTile[] Walls, FloorTile[] Floors, int width, int height, List<VMRoom> rooms) //for first floor gen, curRoom should be 1. For floors above, it should be the last genmap result
        {
            Map = new uint[width*height]; //although 0 is the base of the array, room 1 is known to simantics as room 0.
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
                        ExpectedTile = Floors[i].Pattern;
                        if (ExpectedTile == 0 && noFloorBad) continue;
                        if (ExpectedTile < 65534) ExpectedTile = 0;
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
                    ushort area = 0;
                    while (spread.Count > 0)
                    {
                        area++;
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

                        //

                        if (((PXWalls.Segments & WallSegments.TopLeft) == 0 || PXWalls.TopLeftStyle != 1))
                            SpreadOnto(Walls, Floors, plusX, item.Y, 0, Map, width, height, spread, (ushort)rooms.Count, ExpectedTile, noFloorBad);

                        if (((mainWalls.Segments & WallSegments.TopLeft) == 0 || mainWalls.TopLeftStyle != 1))
                            SpreadOnto(Walls, Floors, minX, item.Y, 2, Map, width, height, spread, (ushort)rooms.Count, ExpectedTile, noFloorBad);

                        if (((PYWalls.Segments & WallSegments.TopRight) == 0 || PYWalls.TopRightStyle != 1))
                            SpreadOnto(Walls, Floors, item.X, plusY, 1, Map, width, height, spread, (ushort)rooms.Count, ExpectedTile, noFloorBad);

                        if (((mainWalls.Segments & WallSegments.TopRight) == 0 || mainWalls.TopRightStyle != 1))
                            SpreadOnto(Walls, Floors, item.X, minY, 3, Map, width, height, spread, (ushort)rooms.Count, ExpectedTile, noFloorBad);
                    }

                    var bounds = new Rectangle(rminX, rminY, (rmaxX - rminX) + 1, (rmaxY - rminY) + 1);
                    var roomObs = GenerateRoomObs((ushort)rooms.Count, bounds);
                    OptimizeObstacles(wallObs);
                    OptimizeObstacles(roomObs);
                    rooms.Add(new VMRoom
                    {
                        IsOutside = outside || ExpectedTile > 65533,
                        IsPool = ExpectedTile > 65533,
                        Bounds = bounds,
                        WallObs = wallObs,
                        RoomObs = roomObs,
                        Area = area
                    });
                    outside = false;
                }
            }
        }

        private static void SpreadOnto(WallTile[] walls, FloorTile[] floors, int x, int y, int inDir, uint[] map, int width, int height, Stack<Point> spread, ushort room, ushort expectedTile, bool noAir)
        {
            var wall = walls[x + y * width];
            var floor = floors[x + y * width].Pattern;

            if ((expectedTile > 0 && expectedTile != floor) || (expectedTile == 0 && floor > 65533))
            {
                return;
            }

            if ((wall.Segments & WallSegments.HorizontalDiag) > 0)
            {
                if (inDir < 2)
                {
                    //bottom (bottom right pattern)
                    if ((map[x + y * width] & (uint)0x0000FFFF)>0) return; //don't spread onto
                    map[x + y * width] |= room;
                    
                }
                else
                {
                    //top (bottom left pattern)
                    if ((map[x + y * width] & (uint)0xFFFF0000) > 0) return; //don't spread onto
                    map[x + y * width] |= (uint)room<<16;
                }
                //if (!floorMode) walls[x + y * width] = wall;
            }
            else if ((wall.Segments & WallSegments.VerticalDiag) > 0)
            {
                if (inDir > 0 && inDir < 3)
                {
                    //left
                    if ((map[x + y * width] & (uint)0x0000FFFF) > 0) return; //don't spread onto
                    map[x + y * width] |= room;
                }
                else
                {
                    //right
                    if ((map[x + y * width] & (uint)0xFFFF0000) > 0) return; //don't spread onto
                    map[x + y * width] |= (uint)room << 16;
                }
                //if (!floorMode) walls[x + y * width] = wall;
            }
            else
            {
                if (map[x + y * width] > 0) return;
                map[x + y * width] = (uint)(room | (room<<16));
            }

            spread.Push(new Point(x, y));
        }

        public List<VMObstacle> GenerateRoomObs(ushort room, Rectangle bounds)
        {
            var result = new List<VMObstacle>();
            var x1 = Math.Max(0, bounds.X - 1);
            var x2 = Math.Min(Width, bounds.Right + 1);
            var y1 = Math.Max(0, bounds.Y - 1);
            var y2 = Math.Min(Height, bounds.Bottom + 1);

            for (int y = y1; y < y2; y++)
            {
                VMObstacle next = null;
                for (int x = x1; x < x2; x++)
                {
                    int tRoom = (ushort)Map[x + y * Width];
                    if (tRoom != room)
                    {
                        if (next != null) next.x2 += 16;
                        else
                        {
                            next = new VMObstacle((x << 4) - 3, (y << 4) - 3, (x << 4) + 19, (y << 4) + 19);
                            result.Add(next);
                        }
                    }
                    else
                    {
                        if (next != null) next = null;
                    }
                }
            }
            return result;
        }

        public void OptimizeObstacles(List<VMObstacle> obs)
        {
            // collapses axis aligned collision bounds into single rectangles (useful for walls and rooms)
            // i have totally verified that this is reliable and optimal in every percievable way
            // O(n^2) probably
            // if you find a better way please replace, this is just sort of helpful.

            var changed = new LinkedList<VMObstacle>(obs);

            var item = changed.First;
            while (item != null)
            {
                var r1 = item.Value;
                //ok, so for each rectangle we need to find rectangles we can join with.
                var targ = changed.First;
                while (targ != null)
                {
                    var r2 = targ.Value;
                    var nextT = targ.Next;
                    if (r1 != r2)
                    {
                        if (r1.x1 == r2.x1 && r1.x2 == r2.x2 && !(r1.y1 > r2.y2 || r1.y2 < r2.y1))
                        {
                            //intersects... combine em
                            r1.y1 = Math.Min(r1.y1, r2.y1);
                            r1.y2 = Math.Max(r1.y2, r2.y2);
                            changed.Remove(targ);
                            obs.Remove(r2);
                        }
                        else if (r1.y1 == r2.y1 && r1.y2 == r2.y2 && !(r1.x1 > r2.x2 || r1.x2 < r2.x1))
                        {
                            r1.x1 = Math.Min(r1.x1, r2.x1);
                            r1.x2 = Math.Max(r1.x2, r2.x2);
                            changed.Remove(targ);
                            obs.Remove(r2);
                        }
                    }

                    targ = nextT;
                }
                item = item.Next;
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
