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
using FSO.LotView.Model;

namespace FSO.SimAntics.Utils
{
    public static class VMArchitectureTools
    {
        private static Point[] WLStartOff = {
            
            // Look at this way up <----
            // Starting at % line, going cw. Middle is (0,0), and below it is the tile (0,0)..
            //
            //        /\
            //       /  \ +x
            //      /\  %\
            //     /  \%  \
            //     \  /\  /
            //      \/  \/
            //       \  / +y
            //        \/

            new Point(0, 0),
            new Point(0, 0),
            new Point(0, 0),
            new Point(-1, 0),
            new Point(-1, 0),
            new Point(-1, -1),
            new Point(0, -1),
            new Point(0, -1),
        };

        private static WallSegments[] WLMainSeg =
        {
            WallSegments.TopRight,
            WallSegments.VerticalDiag,
            WallSegments.TopLeft,
            WallSegments.HorizontalDiag,
            WallSegments.TopRight,
            WallSegments.VerticalDiag,
            WallSegments.TopLeft,
            WallSegments.HorizontalDiag
        };

        private static Point[] WLSubOff =
        {
            new Point(0, -1),
            new Point(-1, 0),
            new Point(0, -1),
            new Point(-1, 0),
        };

        private static WallSegments[] WLSubSeg =
        {
            WallSegments.BottomLeft,
            WallSegments.BottomRight,
            WallSegments.BottomLeft,
            WallSegments.BottomRight,
        };

        private static Point[] WLStep =
        {
            new Point(1, 0),
            new Point(1, 1),
            new Point(0, 1),
            new Point(-1, 1),
            new Point(-1, 0),
            new Point(-1, -1),
            new Point(0, -1),
            new Point(1, -1),
        };

        private static WallSegments AnyDiag = WallSegments.HorizontalDiag | WallSegments.VerticalDiag;

        //things 2 note
        //default style is 1
        //default pattern is 0
        //mid drawing pattern/style is 255
        public static int DrawWall(VMArchitecture target, Point pos, int length, int direction, ushort pattern, ushort style, sbyte level, bool force)
        {
            if (!force && !VerifyDrawWall(target, pos, length, direction, level)) return 0;

            int totalWalls = 0;
            pos += WLStartOff[direction];
            bool diagCheck = (direction % 2 == 1);
            for (int i=0; i<length; i++)
            {
                var wall = target.GetWall((short)pos.X, (short)pos.Y, level);
                if ((wall.Segments & (WLMainSeg[direction] | AnyDiag)) == 0 && (!diagCheck || (wall.Segments == 0)))
                {
                    //no wall here already, apply it.
                    wall.Segments |= WLMainSeg[direction];
                    if (diagCheck)
                    {
                        wall.TopRightStyle = style;
                        wall.BottomLeftPattern = pattern;
                        wall.BottomRightPattern = pattern;
                    }
                    else if (WLMainSeg[direction] == WallSegments.TopRight)
                    {
                        wall.TopRightStyle = style;
                        wall.TopRightPattern = pattern;  
                    }
                    else
                    {
                        wall.TopLeftStyle = style;
                        wall.TopLeftPattern = pattern;
                    }

                    totalWalls++;
                    target.SetWall((short)pos.X, (short)pos.Y, level, wall);

                    if (!diagCheck)
                    {
                        var tPos = pos + WLSubOff[direction / 2];
                        wall = target.GetWall((short)tPos.X, (short)tPos.Y, level);
                        wall.Segments |= WLSubSeg[direction / 2];

                        if (WLSubSeg[direction / 2] == WallSegments.BottomRight) wall.BottomRightPattern = pattern;
                        else wall.BottomLeftPattern = pattern;

                        target.SetWall((short)tPos.X, (short)tPos.Y, level, wall);
                    }
                }
                pos += WLStep[direction];
            }
            return totalWalls;
        }

        public static int EraseWall(VMArchitecture target, Point pos, int length, int direction, ushort pattern, ushort style, sbyte level)
        {
            if (!VerifyEraseWall(target, pos, length, direction, level)) return 0;

            int totalWalls = 0;
            pos += WLStartOff[direction];
            bool diagCheck = (direction % 2 == 1);
            for (int i = 0; i < length; i++)
            {
                var wall = target.GetWall((short)pos.X, (short)pos.Y, level);
                if ((wall.Segments & WLMainSeg[direction]) > 0) totalWalls++;
                wall.Segments &= ~WLMainSeg[direction];               
                target.SetWall((short)pos.X, (short)pos.Y, level, wall);

                if (!diagCheck)
                {
                    var tPos = pos + WLSubOff[direction / 2];
                    wall = target.GetWall((short)tPos.X, (short)tPos.Y, level);
                    wall.Segments &= ~WLSubSeg[direction / 2];

                    target.SetWall((short)tPos.X, (short)tPos.Y, level, wall);
                }
                pos += WLStep[direction];
            }
            return totalWalls;
        }

        public static bool VerifyEraseWall(VMArchitecture target, Point pos, int length, int direction, sbyte level)
        {
            pos += WLStartOff[direction];
            bool diagCheck = (direction % 2 == 1);
            for (int i = 0; i < length; i++)
            {
                if (pos.X <= 0 || pos.X >= target.Width || pos.Y <= 0 || pos.Y >= target.Height) return false;
                var wall = target.GetWall((short)pos.X, (short)pos.Y, level);
                wall.Segments &= ~WLMainSeg[direction];
                if (!target.Context.CheckWallValid(LotTilePos.FromBigTile((short)pos.X, (short)pos.Y, level), wall)) return false;

                if (!diagCheck)
                {
                    var tPos = pos + WLSubOff[direction / 2];
                    wall = target.GetWall((short)tPos.X, (short)tPos.Y, level);
                    wall.Segments &= ~WLSubSeg[direction / 2];

                    if (!target.Context.CheckWallValid(LotTilePos.FromBigTile((short)tPos.X, (short)tPos.Y, level), wall)) return false;
                }
                pos += WLStep[direction];
            }
            return true;
        }

        public static bool VerifyDrawWall(VMArchitecture target, Point pos, int length, int direction, sbyte level)
        {
            pos += WLStartOff[direction];
            bool diagCheck = (direction % 2 == 1);
            for (int i = 0; i < length; i++)
            {
                if (pos.X <= 0 || pos.X >= target.Width || pos.Y <= 0 || pos.Y >= target.Height) return false;
                var wall = target.GetWall((short)pos.X, (short)pos.Y, level);
                if ((wall.Segments & AnyDiag) == 0 && (!diagCheck || (wall.Segments == 0)))
                {
                    wall.Segments &= ~WLMainSeg[direction];
                    if (!target.Context.CheckWallValid(LotTilePos.FromBigTile((short)pos.X, (short)pos.Y, level), wall)) return false;
                    if (!diagCheck)
                    {
                        var tPos = pos + WLSubOff[direction / 2];
                        wall = target.GetWall((short)tPos.X, (short)tPos.Y, level);
                        if (!(level == 1 || target.Supported[level - 2][pos.Y * target.Width + pos.X] || target.Supported[level - 2][tPos.Y * target.Width + tPos.X])) return false;
                        if ((wall.Segments & AnyDiag) == 0)
                        {
                            wall.Segments |= WLSubSeg[direction / 2];
                            if (!target.Context.CheckWallValid(LotTilePos.FromBigTile((short)tPos.X, (short)tPos.Y, level), wall)) return false;
                        }
                        else return false;
                    } else
                    {
                        if (!(level == 1 || target.Supported[level - 2][pos.Y * target.Width + pos.X])) return false;
                    }
                }
                else return false;
                pos += WLStep[direction];
            }
            return true;
        }

        public static int DrawWallRect(VMArchitecture target, Rectangle rect, ushort pattern, ushort style, sbyte level)
        {
            if (!(
                VerifyDrawWall(target, new Point(rect.X, rect.Y), rect.Width, 0, level) &&
                VerifyDrawWall(target, new Point(rect.X, rect.Y), rect.Height, 2, level) &&
                VerifyDrawWall(target, new Point(rect.X, rect.Y + rect.Height), rect.Width, 0, level) &&
                VerifyDrawWall(target, new Point(rect.X + rect.Width, rect.Y), rect.Height, 2, level)
                )) return 0;
            int totalWalls = 0;
            totalWalls += DrawWall(target, new Point(rect.X, rect.Y), rect.Width, 0, pattern, style, level, true);
            totalWalls += DrawWall(target, new Point(rect.X, rect.Y), rect.Height, 2, pattern, style, level, true);
            totalWalls += DrawWall(target, new Point(rect.X, rect.Y + rect.Height), rect.Width, 0, pattern, style, level, true);
            totalWalls += DrawWall(target, new Point(rect.X + rect.Width, rect.Y), rect.Height, 2, pattern, style, level, true);
            return totalWalls;
        }

        public static int GetPatternDirection(VMArchitecture target, Point pos, ushort pattern, int direction, int altDir, sbyte level)
        {
            if (pos.X < 0 || pos.X >= target.Width || pos.Y < 0 || pos.Y >= target.Height) return -1;

            var wall = target.GetWall((short)pos.X, (short)pos.Y, level);
            if ((wall.Segments & WallSegments.HorizontalDiag) > 0)
            {
                return direction;
            }
            else if ((wall.Segments & WallSegments.VerticalDiag) > 0)
            {
                return direction;
            }

            if ((wall.Segments & (WallSegments)(1 << direction)) > 0) { }
            else if ((wall.Segments & (WallSegments)(1 << altDir)) > 0) direction = altDir;
            else
            {
                return -1;
            }

            return direction;
        }

        public static int WallPatternDot(VMArchitecture target, Point pos, ushort pattern, int direction, int altDir, sbyte level)
        {
            if (pos.X < 0 || pos.X >= target.Width || pos.Y < 0 || pos.Y > target.Height) return -1;

            ushort replaced = 0;
            var wall = target.GetWall((short)pos.X, (short)pos.Y, level);
            //direction starts lefttop, righttop
            if ((wall.Segments & WallSegments.HorizontalDiag) > 0 && wall.TopRightStyle == 1)
            {
                if (direction < 2)
                {
                    //bottom (bottom right pattern)
                    replaced = wall.BottomRightPattern;
                    wall.BottomRightPattern = pattern;
                }
                else
                {
                    //top (bottom left pattern)
                    replaced = wall.BottomLeftPattern;
                    wall.BottomLeftPattern = pattern;
                }
                target.SetWall((short)pos.X, (short)pos.Y, level, wall);
                return (replaced == pattern) ? -1 : direction;
            }
            else if ((wall.Segments & WallSegments.VerticalDiag) > 0 && wall.TopRightStyle == 1)
            {
                if (direction > 0 && direction < 3)
                {
                    //left
                    replaced = wall.BottomLeftPattern;
                    wall.BottomLeftPattern = pattern;
                }
                else
                {
                    //right
                    replaced = wall.BottomRightPattern;
                    wall.BottomRightPattern = pattern;
                }
                target.SetWall((short)pos.X, (short)pos.Y, level, wall);
                return (replaced == pattern) ? -1 : direction;
            }

            if ((wall.Segments & (WallSegments)(1 << direction)) > 0) { }
            else if ((wall.Segments & (WallSegments)(1 << altDir)) > 0) direction = altDir;
            else
            {
                return -1;
            }

            if (direction == 0 && wall.TopLeftThick) { replaced = wall.TopLeftPattern; wall.TopLeftPattern = pattern; }
            else if (direction == 1 && wall.TopRightThick) { replaced = wall.TopRightPattern; wall.TopRightPattern = pattern; }
            else if (direction == 2 && pos.X < target.Width && target.GetWall((short)(pos.X + 1), (short)pos.Y, level).TopLeftThick) { replaced = wall.BottomRightPattern; wall.BottomRightPattern = pattern; }
            else if (direction == 3 && pos.Y < target.Height && target.GetWall((short)pos.X, (short)(pos.Y + 1), level).TopRightThick) { replaced = wall.BottomLeftPattern; wall.BottomLeftPattern = pattern; }
            target.SetWall((short)pos.X, (short)pos.Y, level, wall);
            return (replaced == pattern)?-1:direction;
        }

        /// <summary>
        /// Fills a room with a certain wall pattern. Returns walls covered
        /// </summary>
        public static int WallPatternFill(VMArchitecture target, Point pos, ushort pattern, sbyte level) //for first floor gen, curRoom should be 1. For floors above, it should be the last genmap result
        {
            if (pos.X < 0 || pos.X >= target.Width || pos.Y < 0 || pos.Y >= target.Height) return 0;

            pos.X = Math.Max(Math.Min(pos.X, target.Width-1), 0);
            pos.Y = Math.Max(Math.Min(pos.Y, target.Height-1), 0);
            var walls = target.Walls[level-1];

            var width = target.Width;
            var height = target.Height;
            int wallsCovered = 0;

            byte[] Map = new byte[target.Width * target.Height];

            //flood fill recursively. Each time choose find and choose the first "0" as the base.
            //The first recursion (outside) cannot fill into diagonals.
            var spread = new Stack<Point>();
            spread.Push(pos);
            while (spread.Count > 0)
            {
                var item = spread.Pop();

                var plusX = (item.X + 1) % width;
                var minX = (item.X + width - 1) % width;
                var plusY = (item.Y + 1) % height;
                var minY = (item.Y + height - 1) % height;

                var mainWalls = walls[item.X + item.Y * width];
                if ((byte)mainWalls.Segments > 15) continue; //don't spread on diagonals for now

                var PXWalls = walls[plusX + item.Y * width];
                var PYWalls = walls[item.X + plusY * width];

                if (Map[plusX + item.Y * width] < 3 && ((PXWalls.Segments & WallSegments.TopLeft) == 0 || PXWalls.TopLeftStyle != 1))
                    wallsCovered += SpreadOnto(walls, plusX, item.Y, 0, Map, width, height, spread, pattern, false);
                else
                {
                    if (mainWalls.BottomRightPattern != pattern && PXWalls.TopLeftThick)
                    {
                        wallsCovered++;
                        mainWalls.BottomRightPattern = pattern;
                    }
                }

                if (Map[minX + item.Y * width]<3 && ((mainWalls.Segments & WallSegments.TopLeft) == 0 || mainWalls.TopLeftStyle != 1))
                    wallsCovered += SpreadOnto(walls, minX, item.Y, 2, Map, width, height, spread, pattern, false);
                else
                {
                    if (mainWalls.TopLeftPattern != pattern && mainWalls.TopLeftThick)
                    {
                        wallsCovered++;
                        mainWalls.TopLeftPattern = pattern;
                    }
                }

                if (Map[item.X + plusY * width]<3 && ((PYWalls.Segments & WallSegments.TopRight) == 0 || PYWalls.TopRightStyle != 1))
                    wallsCovered += SpreadOnto(walls, item.X, plusY, 1, Map, width, height, spread, pattern, false);
                else
                {
                    if (mainWalls.BottomLeftPattern != pattern && PYWalls.TopRightThick)
                    {
                        wallsCovered++;
                        mainWalls.BottomLeftPattern = pattern;
                    }
                }

                if (Map[item.X + minY * width]<3 && ((mainWalls.Segments & WallSegments.TopRight) == 0 || mainWalls.TopRightStyle != 1))
                    wallsCovered += SpreadOnto(walls, item.X, minY, 3, Map, width, height, spread, pattern, false);
                else
                {
                    if (mainWalls.TopRightPattern != pattern && mainWalls.TopRightThick)
                    {
                        wallsCovered++;
                        mainWalls.TopRightPattern = pattern;
                    }
                }

                walls[item.X + item.Y * width] = mainWalls;
            }
            return wallsCovered;
        }

        private static int SpreadOnto(WallTile[] walls, int x, int y, int inDir, byte[] map, int width, int height, Stack<Point> spread, ushort pattern, bool floorMode)
        {
            int filled = 0;
            var wall = walls[x + y * width];
            if ((wall.Segments & WallSegments.HorizontalDiag) > 0 && (wall.TopRightStyle == 1 || floorMode))
            {
                if (inDir < 2)
                {
                    //bottom (bottom right pattern)
                    if (!floorMode && wall.BottomLeftPattern != pattern)
                    {
                        filled++;
                        wall.BottomLeftPattern = pattern;
                    }
                    map[x + y * width] |= 1;
                } else
                {
                    //top (bottom left pattern)
                    if (!floorMode && wall.BottomRightPattern != pattern)
                    {
                        filled++;
                        wall.BottomRightPattern = pattern;
                    }
                    map[x + y * width] |= 2;
                }
                if (!floorMode) walls[x + y * width] = wall;
            }
            else if ((wall.Segments & WallSegments.VerticalDiag) > 0 && (wall.TopRightStyle == 1 || floorMode))
            {
                if (inDir > 0 && inDir < 3)
                {
                    //left
                    if (!floorMode && wall.BottomRightPattern != pattern)
                    {
                        filled++;
                        wall.BottomRightPattern = pattern;
                    }
                    map[x + y * width] |= 1;
                }
                else
                {
                    //right
                    if (!floorMode && wall.BottomLeftPattern != pattern)
                    {
                        filled++;
                        wall.BottomLeftPattern = pattern;
                    }
                    map[x + y * width] |= 2;
                }
                if (!floorMode) walls[x + y * width] = wall;
            }
            else
            {
                map[x + y * width] = 3;
            }
            
            spread.Push(new Point(x, y));
            return filled;
        }

        /// <summary>
        /// Fills a room with a certain Floor pattern. Returns floors covered
        /// </summary>
        public static int FloorPatternFill(VMArchitecture target, Point pos, ushort pattern, sbyte level) //for first floor gen, curRoom should be 1. For floors above, it should be the last genmap result
        {
            if (pattern > 65533 || pos.X < 0 || pos.X >= target.Width || pos.Y < 0 || pos.Y >= target.Height) return 0;

            pos.X = Math.Max(Math.Min(pos.X, target.Width - 1), 0);
            pos.Y = Math.Max(Math.Min(pos.Y, target.Height - 1), 0);
            var walls = target.Walls[level-1];

            var width = target.Width;
            var height = target.Height;
            int floorsCovered = 0;

            byte[] Map = new byte[target.Width * target.Height];

            //flood fill recursively. Each time choose find and choose the first "0" as the base.
            //The first recursion (outside) cannot fill into diagonals.
            var spread = new Stack<Point>();
            spread.Push(pos);
            while (spread.Count > 0)
            {
                var item = spread.Pop();

                var plusX = (item.X + 1) % width;
                var minX = (item.X + width - 1) % width;
                var plusY = (item.Y + 1) % height;
                var minY = (item.Y + height - 1) % height;

                var mainWalls = walls[item.X + item.Y * width];
                var floor = target.GetFloor((short)item.X, (short)item.Y, level);
                if ((byte)mainWalls.Segments > 15)
                {
                    //draw floor onto a diagonal;
                    var wall = walls[item.X + item.Y * width];
                    byte flags = Map[item.X + item.Y * width];

                    if (flags == 3) continue;
                    if ((mainWalls.Segments & WallSegments.HorizontalDiag) > 0) flags = (byte)(3 - flags);

                        if ((flags & 1) == 1 && wall.TopLeftPattern != pattern)
                        {
                            floorsCovered++;
                            wall.TopLeftPattern = pattern;
                        }
                        else if ((flags & 2) == 2 && wall.TopLeftStyle != pattern)
                        {
                            floorsCovered++;
                            wall.TopLeftStyle = pattern;
                        }

                    walls[item.X + item.Y * width] = wall;
                    continue; //don't spread on diagonals for now
                }
                else
                {
                    //normal tile, draw a floor here.
                    if (floor.Pattern != pattern)
                    {
                        floor.Pattern = pattern;
                        if (target.SetFloor((short)item.X, (short)item.Y, level, floor, false)) floorsCovered += 2;
                    }

                }


                if (Map[plusX + item.Y * width] < 3 && (mainWalls.Segments & WallSegments.BottomRight) == 0)
                    SpreadOnto(walls, plusX, item.Y, 0, Map, width, height, spread, pattern, true);

                if (Map[minX + item.Y * width] < 3 && (mainWalls.Segments & WallSegments.TopLeft) == 0)
                    SpreadOnto(walls, minX, item.Y, 2, Map, width, height, spread, pattern, true);

                if (Map[item.X + plusY * width] < 3 && (mainWalls.Segments & WallSegments.BottomLeft) == 0)
                    SpreadOnto(walls, item.X, plusY, 1, Map, width, height, spread, pattern, true);
                
                if (Map[item.X + minY * width] < 3 && (mainWalls.Segments & WallSegments.TopRight) == 0)
                    SpreadOnto(walls, item.X, minY, 3, Map, width, height, spread, pattern, true);

                walls[item.X + item.Y * width] = mainWalls;
            }
            return floorsCovered;
        }

        public static int FloorPatternRect(VMArchitecture target, Rectangle rect, ushort dir, ushort pattern, sbyte level) //returns floors covered
        {
            int floorsCovered = 0;
            if (rect.Width == 0 && rect.Height == 0)
            {
                //dot mode, just fill a tile. can be a diagonal.
                if (rect.X < 0 || rect.X >= target.Width || rect.Y < 0 || rect.Y >= target.Width) return 0;
                var wall = target.GetWall((short)rect.X, (short)rect.Y, level);
                if ((wall.Segments & AnyDiag) > 0 && pattern < 65534)
                {
                    bool side = ((wall.Segments & WallSegments.HorizontalDiag) > 0) ? (dir < 2) : (dir < 1 || dir > 2);
                    if (side)
                    {
                        if (wall.TopLeftStyle != pattern)
                        {
                            floorsCovered++;
                            wall.TopLeftStyle = pattern;
                        }
                    }
                    else
                    {
                        if (wall.TopLeftPattern != pattern)
                        {
                            floorsCovered++;
                            wall.TopLeftPattern = pattern;
                        }
                    }
                    target.SetWall((short)rect.X, (short)rect.Y, level, wall);
                }
                else if ((wall.Segments & AnyDiag) == 0)
                {
                    var floor = target.GetFloor((short)rect.X, (short)rect.Y, level);
                    if (floor.Pattern != pattern)
                    {
                        floor.Pattern = pattern;
                        if (target.SetFloor((short)rect.X, (short)rect.Y, level, floor, false)) floorsCovered += 2;
                    }
                }
                return floorsCovered;
            }

            var xEnd = Math.Min(target.Width, rect.X + rect.Width+1);
            var yEnd = Math.Min(target.Height, rect.Y + rect.Height+1);
            for (int y = Math.Max(0, rect.Y); y < yEnd; y++)
            {
                for (int x = Math.Max(0, rect.X); x < xEnd; x++)
                {
                    var wall = target.GetWall((short)x, (short)y, level);
                    if ((wall.Segments & AnyDiag) > 0) //diagonal floors are stored in walls
                    {
                        if (pattern < 65534) continue;
                        if (wall.TopLeftStyle != pattern)
                        {
                            wall.TopLeftStyle = pattern;
                            floorsCovered++;
                        }

                        if (wall.TopLeftPattern != pattern)
                        {
                            wall.TopLeftPattern = pattern;
                            floorsCovered++;
                        }
                        target.SetWall((short)x, (short)y, level, wall);
                    }
                    else
                    {
                        var floor = target.GetFloor((short)x, (short)y, level);
                        if (floor.Pattern != pattern)
                        {
                            floor.Pattern = pattern;
                            if (target.SetFloor((short)x, (short)y, level, floor, false)) floorsCovered += 2;
                        }
                    }
                }
            }
            return floorsCovered;
        }

    }
}
