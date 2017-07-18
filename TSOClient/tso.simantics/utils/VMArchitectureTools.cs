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
using FSO.Content;
using FSO.LotView;

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
                        var floor = target.GetFloor((short)pos.X, (short)pos.Y, level);
                        wall.TopRightStyle = style;
                        wall.BottomLeftPattern = pattern;
                        wall.BottomRightPattern = pattern;
                        wall.TopLeftStyle = floor.Pattern;
                        wall.TopLeftPattern = floor.Pattern;
                        floor.Pattern = 0;
                        target.SetFloor((short)pos.X, (short)pos.Y, level, floor, true);
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
                if (target.OutsideClip((short)pos.X, (short)pos.Y, level)) return false;
                var wall = target.GetWall((short)pos.X, (short)pos.Y, level);
                wall.Segments &= ~WLMainSeg[direction];
                if (!target.Context.CheckWallValid(LotTilePos.FromBigTile((short)pos.X, (short)pos.Y, level), wall)) return false;

                if (!diagCheck)
                {
                    var tPos = pos + WLSubOff[direction / 2]; //remove other side
                    if (target.OutsideClip((short)tPos.X, (short)tPos.Y, level)) return false;
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
                if (target.OutsideClip((short)pos.X, (short)pos.Y, level)) return false;
                var wall = target.GetWall((short)pos.X, (short)pos.Y, level);
                if ((wall.Segments & AnyDiag) == 0 && (!diagCheck || (wall.Segments == 0)))
                {
                    wall.Segments |= WLMainSeg[direction];
                    if (!target.Context.CheckWallValid(LotTilePos.FromBigTile((short)pos.X, (short)pos.Y, level), wall)) return false;
                    if (!diagCheck)
                    {
                        var tPos = pos + WLSubOff[direction / 2]; //get the other side of the wall
                        if (target.OutsideClip((short)tPos.X, (short)tPos.Y, level)) return false; //both sides of wall must be in bounds
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
            if(target.OutsideClip((short)pos.X, (short)pos.Y, level)) return -1;

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

        public static PatternReplaceCount WallPatternDot(VMArchitecture target, Point pos, ushort pattern, int direction, int altDir, sbyte level)
        {
            if (target.OutsideClip((short)pos.X, (short)pos.Y, level)) return new PatternReplaceCount { Total = -1 };

            //pattern replace count used a little differently here. cost still stores replaced cost, but total stores replaced direction.
            PatternReplaceCount replaceCost = new PatternReplaceCount(false);
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
                if (replaced == pattern) return new PatternReplaceCount { Total = -1 };
                replaceCost.Add(replaced);
                replaceCost.Total = direction;
                return replaceCost;
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
                if (replaced == pattern) return new PatternReplaceCount { Total = -1 };
                replaceCost.Add(replaced);
                replaceCost.Total = direction;
                return replaceCost;
            }

            if ((wall.Segments & (WallSegments)(1 << direction)) > 0) { }
            else if ((wall.Segments & (WallSegments)(1 << altDir)) > 0) direction = altDir;
            else
            {
                return new PatternReplaceCount { Total = -1 };
            }

            if (direction == 0 && wall.TopLeftThick) { replaced = wall.TopLeftPattern; wall.TopLeftPattern = pattern; }
            else if (direction == 1 && wall.TopRightThick) { replaced = wall.TopRightPattern; wall.TopRightPattern = pattern; }
            else if (direction == 2 && pos.X < target.Width && target.GetWall((short)(pos.X + 1), (short)pos.Y, level).TopLeftThick) { replaced = wall.BottomRightPattern; wall.BottomRightPattern = pattern; }
            else if (direction == 3 && pos.Y < target.Height && target.GetWall((short)pos.X, (short)(pos.Y + 1), level).TopRightThick) { replaced = wall.BottomLeftPattern; wall.BottomLeftPattern = pattern; }
            target.SetWall((short)pos.X, (short)pos.Y, level, wall);

            if (replaced == pattern) return new PatternReplaceCount { Total = -1 };
            replaceCost.Add(replaced);
            replaceCost.Total = direction;
            return replaceCost;
        }

        /// <summary>
        /// Fills a room with a certain wall pattern. Returns walls covered
        /// </summary>
        public static PatternReplaceCount WallPatternFill(VMArchitecture target, Point pos, ushort pattern, sbyte level) //for first floor gen, curRoom should be 1. For floors above, it should be the last genmap result
        {
            if (target.OutsideClip((short)pos.X, (short)pos.Y, level)) return new PatternReplaceCount(); //can't start OOB
            var walls = target.Walls[level-1];

            var width = target.Width;
            var height = target.Height;
            PatternReplaceCount wallsCovered = new PatternReplaceCount(false);

            byte[] Map = new byte[target.Width * target.Height];

            //flood fill recursively. Each time choose find and choose the first "0" as the base.
            //The first recursion (outside) cannot fill into diagonals.
            var spread = new Stack<Point>();
            spread.Push(pos);
            while (spread.Count > 0)
            {
                var item = spread.Pop();
                if (target.OutsideClip((short)item.X, (short)item.Y, level)) continue; //do not spread into OOB

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
                        wallsCovered.Add(mainWalls.BottomRightPattern);
                        mainWalls.BottomRightPattern = pattern;
                    }
                }

                if (Map[minX + item.Y * width]<3 && ((mainWalls.Segments & WallSegments.TopLeft) == 0 || mainWalls.TopLeftStyle != 1))
                    wallsCovered += SpreadOnto(walls, minX, item.Y, 2, Map, width, height, spread, pattern, false);
                else
                {
                    if (mainWalls.TopLeftPattern != pattern && mainWalls.TopLeftThick)
                    {
                        wallsCovered.Add(mainWalls.TopLeftPattern);
                        mainWalls.TopLeftPattern = pattern;
                    }
                }

                if (Map[item.X + plusY * width]<3 && ((PYWalls.Segments & WallSegments.TopRight) == 0 || PYWalls.TopRightStyle != 1))
                    wallsCovered += SpreadOnto(walls, item.X, plusY, 1, Map, width, height, spread, pattern, false);
                else
                {
                    if (mainWalls.BottomLeftPattern != pattern && PYWalls.TopRightThick)
                    {
                        wallsCovered.Add(mainWalls.BottomLeftPattern);
                        mainWalls.BottomLeftPattern = pattern;
                    }
                }

                if (Map[item.X + minY * width]<3 && ((mainWalls.Segments & WallSegments.TopRight) == 0 || mainWalls.TopRightStyle != 1))
                    wallsCovered += SpreadOnto(walls, item.X, minY, 3, Map, width, height, spread, pattern, false);
                else
                {
                    if (mainWalls.TopRightPattern != pattern && mainWalls.TopRightThick)
                    {
                        wallsCovered.Add(mainWalls.TopRightPattern);
                        mainWalls.TopRightPattern = pattern;
                    }
                }

                walls[item.X + item.Y * width] = mainWalls;
            }
            return wallsCovered;
        }

        private static PatternReplaceCount SpreadOnto(WallTile[] walls, int x, int y, int inDir, byte[] map, int width, int height, Stack<Point> spread, ushort pattern, bool floorMode)
        {
            PatternReplaceCount filled = new PatternReplaceCount(false);
            var wall = walls[x + y * width];
            if ((wall.Segments & WallSegments.HorizontalDiag) > 0 && (wall.TopRightStyle == 1 || floorMode))
            {
                if (inDir < 2)
                {
                    //bottom (bottom right pattern)
                    if (!floorMode && wall.BottomLeftPattern != pattern)
                    {
                        filled.Add(wall.BottomLeftPattern);
                        wall.BottomLeftPattern = pattern;
                    }
                    map[x + y * width] |= 1;
                } else
                {
                    //top (bottom left pattern)
                    if (!floorMode && wall.BottomRightPattern != pattern)
                    {
                        filled.Add(wall.BottomRightPattern);
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
                        filled.Add(wall.BottomRightPattern);
                        wall.BottomRightPattern = pattern;
                    }
                    map[x + y * width] |= 1;
                }
                else
                {
                    //right
                    if (!floorMode && wall.BottomLeftPattern != pattern)
                    {
                        filled.Add(wall.BottomLeftPattern);
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
        public static PatternReplaceCount FloorPatternFill(VMArchitecture target, Point pos, ushort pattern, sbyte level) //for first floor gen, curRoom should be 1. For floors above, it should be the last genmap result
        {
            if (pattern > 65533 || target.OutsideClip((short)pos.X, (short)pos.Y, level)) return new PatternReplaceCount();

            //you cannot start out of bounds. spread is limited by bounds
            var walls = target.Walls[level-1];

            var width = target.Width;
            var height = target.Height;
            PatternReplaceCount floorsCovered = new PatternReplaceCount(true);

            byte[] Map = new byte[target.Width * target.Height];

            //flood fill recursively. Each time choose find and choose the first "0" as the base.
            //The first recursion (outside) cannot fill into diagonals.
            var spread = new Stack<Point>();
            spread.Push(pos);
            while (spread.Count > 0)
            {
                var item = spread.Pop();
                if (target.OutsideClip((short)item.X, (short)item.Y, level)) continue; //do not spread into OOB

                var plusX = (item.X + 1) % width;
                var minX = (item.X + width - 1) % width;
                var plusY = (item.Y + 1) % height;
                var minY = (item.Y + height - 1) % height;

                var mainWalls = walls[item.X + item.Y * width];
                if ((byte)mainWalls.Segments > 15)
                {
                    //draw floor onto a diagonal;
                    var wall = walls[item.X + item.Y * width];
                    byte flags = Map[item.X + item.Y * width];

                    if (flags == 3) continue;
                    if ((mainWalls.Segments & WallSegments.HorizontalDiag) > 0) flags = (byte)(3 - flags);

                        if ((flags & 1) == 1 && wall.TopLeftPattern != pattern)
                        {
                            floorsCovered.Add(wall.TopLeftPattern);
                            wall.TopLeftPattern = pattern;
                        }
                        else if ((flags & 2) == 2 && wall.TopLeftStyle != pattern)
                        {
                            floorsCovered.Add(wall.TopLeftStyle);
                            wall.TopLeftStyle = pattern;
                        }

                    walls[item.X + item.Y * width] = wall;
                    continue; //don't spread on diagonals for now
                }
                else
                {
                    //normal tile, draw a floor here.
                    var floor = target.GetFloor((short)item.X, (short)item.Y, level);
                    if (floor.Pattern != pattern)
                    {
                        var old = floor.Pattern;
                        floor.Pattern = pattern;
                        if (target.SetFloor((short)item.X, (short)item.Y, level, floor, false)) floorsCovered.DAdd(old);
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

        public static PatternReplaceCount FloorPatternRect(VMArchitecture target, Rectangle rect, ushort dir, ushort pattern, sbyte level) //returns floors covered
        {
            PatternReplaceCount floorsCovered = new PatternReplaceCount(true);
            if (rect.Width == 0 && rect.Height == 0)
            {
                //dot mode, just fill a tile. can be a diagonal.
                if (target.OutsideClip((short)rect.X, (short)rect.Y, level)) return floorsCovered; //out of bounds
                var wall = target.GetWall((short)rect.X, (short)rect.Y, level);
                if ((wall.Segments & AnyDiag) > 0 && pattern < 65534)
                {
                    bool side = ((wall.Segments & WallSegments.HorizontalDiag) > 0) ? (dir < 2) : (dir < 1 || dir > 2);
                    if (side)
                    {
                        if (wall.TopLeftStyle != pattern)
                        {
                            floorsCovered.Add(wall.TopLeftStyle);
                            wall.TopLeftStyle = pattern;
                        }
                    }
                    else
                    {
                        if (wall.TopLeftPattern != pattern)
                        {
                            floorsCovered.Add(wall.TopLeftPattern);
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
                        var old = floor.Pattern;
                        floor.Pattern = pattern;
                        if (target.SetFloor((short)rect.X, (short)rect.Y, level, floor, false)) floorsCovered.DAdd(old);
                    }
                }
                return floorsCovered;
            }

            if (level > (target.DisableClip?target.Stories:target.BuildableFloors)) return floorsCovered; //level out of bounds
            var bounds = target.DisableClip ? new Rectangle(0, 0, target.Width, target.Height):target.BuildableArea;
            var xEnd = Math.Min(bounds.Right, rect.X + rect.Width+1);
            var yEnd = Math.Min(bounds.Bottom, rect.Y + rect.Height+1);
            for (int y = Math.Max(bounds.Y, rect.Y); y < yEnd; y++)
            {
                for (int x = Math.Max(bounds.X, rect.X); x < xEnd; x++)
                {
                    var wall = target.GetWall((short)x, (short)y, level);
                    if ((wall.Segments & AnyDiag) > 0) //diagonal floors are stored in walls
                    {
                        if (pattern < 65534) continue;
                        if (wall.TopLeftStyle != pattern)
                        {
                            wall.TopLeftStyle = pattern;
                            floorsCovered.Add(wall.TopLeftStyle);
                        }

                        if (wall.TopLeftPattern != pattern)
                        {
                            wall.TopLeftPattern = pattern;
                            floorsCovered.Add(wall.TopLeftPattern);
                        }
                        target.SetWall((short)x, (short)y, level, wall);
                    }
                    else
                    {
                        var floor = target.GetFloor((short)x, (short)y, level);
                        if (floor.Pattern != pattern)
                        {
                            var old = floor.Pattern;
                            floor.Pattern = pattern;
                            if (target.SetFloor((short)x, (short)y, level, floor, false)) floorsCovered.DAdd(old);
                        }
                    }
                }
            }
            return floorsCovered;
        }

        //==== TERRAIN ====

        public static int DotTerrain(VMArchitecture target, Point pos, short mod)
        {
            var bounds = target.DisableClip ? new Rectangle(0, 0, target.Width, target.Height) : target.BuildableArea;
            bounds.X--; bounds.Y--; bounds.Width++; bounds.Height++;
            if (!bounds.Contains(pos)) return 0;
            var current = target.GetTerrainGrass((short)pos.X, (short)pos.Y);
            var n = (byte)Math.Max(0, Math.Min(255, current + mod));
            target.SetTerrainGrass((short)pos.X, (short)pos.Y, n);
            return (Math.Abs(current - n) + 31) / 32;
        }

        public static int RaiseTerrain(VMArchitecture target, Rectangle pos, short height, bool smoothMode)
        {
            //does a sort of flood fill from the start point to ensure the raise is valid
            //when any point height is changed its neighbours are checked for the height difference constraint
            int constrain = 5*10;
            if (smoothMode) constrain = 1*10;
            var bounds = target.DisableClip ? new Rectangle(0, 0, target.Width, target.Height) : target.BuildableArea;
            var tl = target.TerrainLimit;
            bounds = Rectangle.Intersect(bounds, tl);
            bounds.Width--; bounds.Height--;

            if (!pos.Intersects(bounds)) return 0;
            else pos = Rectangle.Intersect(pos, bounds);

            var considered = new Dictionary<Point, short>();
            var stack = new Stack<Point>();

            for (int x=0; x<=pos.Width; x++)
            {
                for (int y=0; y<=pos.Height; y++)
                {
                    var p = pos.Location + new Point(x, y);
                    stack.Push(p);
                    considered.Add(p, height);
                    if (!(target.DisableClip || target.Context.SlopeVertexCheck(p.X, p.Y))) return 0;
                }
            }
            var tr = target.Terrain;
            var firstDiff = tr.Heights[pos.Y * tr.Width + pos.X] - height;

            while (stack.Count > 0)
            {
                var p = stack.Pop();
                //check adjacent points
                var adj = new Point[]
                {
                    p + new Point(-1, 0),
                    p + new Point(1, 0),
                    p + new Point(0, -1),
                    p + new Point(0, 1),
                };
                
                //if any of these are OOB exit early. TODO

                var myHeight = considered[p];

                // check each adjacent height. if it has to be changed, first check if it can be:
                // - if a wall, floor (on any level) or object is preventing this from happening, fail.
                // - else queue its height to be changed and check its adjacent.

                foreach (var a in adj) {
                    if (a.X < 0 || a.Y < 0 || a.X >= tr.Width || a.Y >= tr.Height) return 0;
                    short ht;
                    if (!considered.TryGetValue(a, out ht))
                        ht = tr.Heights[a.Y * tr.Width + a.X];
                    var diff = myHeight - ht;
                    var first = height - ht;

                    if (!target.TerrainLimit.Contains(a))
                    {
                        if (!target.DisableClip && Math.Abs(diff) > 100 * 10) return 0;
                        else continue;
                    }

                    if (diff * first <= 0) continue;

                    if (smoothMode) constrain = Math.Min(5, Math.Max(1, (int)Math.Round(DistanceToRect(a, pos))))*10;

                    if (diff > constrain)
                    {
                        //lower the terrain
                        ht = (short)(myHeight - constrain);
                    }
                    else if (diff < -constrain)
                    {
                        //raise the terrain
                        ht = (short)(myHeight + constrain);
                    }
                    else continue;

                    if (!(target.DisableClip || target.Context.SlopeVertexCheck(a.X, a.Y))) return 0;

                    //we needed to change the height. verify that that is a legal move. (walls, floors, objects demand no slope change)
                    //todo

                    considered[a] = ht; //this can overwrite previous expectations
                    stack.Push(a);
                }
            }

            //actually change the terrain
            int cost = 0;
            foreach (var change in considered)
            {
                var changedBy = Math.Abs(target.GetTerrainHeight((short)change.Key.X, (short)change.Key.Y) - change.Value);
                cost += (changedBy + 9) / 10;
                target.SetTerrainHeight((short)change.Key.X, (short)change.Key.Y, change.Value);
                if (change.Key.X > 0 && change.Key.Y > 0)
                    target.SetTerrainGrass((short)(change.Key.X-1), (short)(change.Key.Y-1), 
                        (byte)Math.Min(255, target.GetTerrainGrass((short)(change.Key.X - 1), (short)(change.Key.Y - 1)) + changedBy*6));
            }

            return cost;
        }

        private static double DistanceToRect(Point pt, Rectangle rect)
        {
            var xDist = 0;
            if (pt.X < rect.Left) xDist = pt.X - rect.Left;
            else if (pt.X > rect.Right) xDist = rect.Right - pt.X;
            var yDist = 0;
            if (pt.Y < rect.Top) yDist = pt.Y - rect.Top;
            else if (pt.Y > rect.Bottom) yDist = rect.Bottom - pt.Y;
            return Math.Sqrt(xDist * xDist + yDist * yDist);
        }

        //==== CUTAWAYS ====

        public static int[][] CutCheckDir =
        {
            new int[] {-1,-1},
            new int[] {-1,1},
            new int[] {1,1},
            new int[] {1,-1}
        };

        public static bool[] GenerateRoomCut(VMArchitecture target, sbyte floor, WorldRotation dir, HashSet<uint> cutRooms)
        {
            var result = new bool[target.Width*target.Height];
            var offset = 0;
            var roommap = target.Rooms[floor-1].Map;
            var cutDir = CutCheckDir[(int)dir];
            var walls = target.Walls[floor - 1];

            var width = target.Width;

            for (int y1=0;y1<target.Height; y1++)
            {
                for (int x1=0; x1<target.Width; x1++)
                {
                    if (walls[offset].Segments == 0
                        && (offset + width < walls.Length && walls[offset + width].Segments == 0)
                        && (offset + 1 < walls.Length && walls[offset + 1].Segments == 0)
                        && (offset - width > 0 && walls[offset - width].Segments == 0)
                        && (offset - 1 > 0 && walls[offset - 1].Segments == 0)
                        )
                    {
                        offset++;
                        continue; //ignore empty tiles as an optimisation
                    }
                    bool cut = false;

                    for (int i=0; i<3; i++)
                    {
                        var x = x1 + ((i == 1) ? cutDir[0] : 0);
                        var y = y1 + ((i == 2) ? cutDir[1] : 0);
                        for (int j=0; j<((i>0)?4:5); j++)
                        {
                            if (x < 0 || x >= target.Width || y < 0 || y >= target.Height) break;
                            if (cutRooms.Contains(roommap[y * target.Width + x] & 65535))
                            {
                                cut = true;
                                break;
                            }
                            x += cutDir[0];
                            y += cutDir[1];
                        }
                        if (cut) break;
                    }
                    result[offset++] = cut;
                }
            }
            return result;
        }

        /// <summary>
        /// Cuts the specified rectangle. Returns true if a *potentially noticable change* occurred as a result. (changed a cut to true on a tile where there is a wall)
        /// </summary>
        /// <param name="target"></param>
        /// <param name="cuts"></param>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static bool ApplyCutRectangle(VMArchitecture target, sbyte floor, bool[] cuts, Rectangle rect)
        {
            var walls = target.Walls[floor - 1];
            var width = target.Width;
            bool change = false;
            for (int x = rect.Left; x < rect.Right; x++)
            {
                for (int y=rect.Top; y < rect.Bottom; y++)
                {
                    if (x < 0 || x >= target.Width || y < 0 || y >= target.Height) continue;
                    var offset = (y * target.Width + x);
                    if (walls[offset].Segments == 0
                        && (offset + width < walls.Length && walls[offset + width].Segments == 0)
                        && (offset + 1 < walls.Length && walls[offset + 1].Segments == 0)
                        && (offset - width > 0 && walls[offset - width].Segments == 0)
                        && (offset - 1 > 0 && walls[offset - 1].Segments == 0)
                        ) continue;

                    if (!cuts[offset])
                    {
                        cuts[offset] = true;
                        change = true;
                    }
                }
            }
            return change;
        }
    }
    
    public struct PatternReplaceCount
    {
        public int Cost;
        public int Total;
        public bool FloorMode;

        public static WorldFloorProvider Floors;
        public static WorldWallProvider Walls;

        public PatternReplaceCount(bool floor) {
            if (Floors == null)
            {
                var content = Content.Content.Get();
                Walls = content.WorldWalls;
                Floors = content.WorldFloors;
            }
            FloorMode = floor; Cost = 0; Total = 0;
        }

        public static PatternReplaceCount operator +(PatternReplaceCount p1, PatternReplaceCount p2)
        {
            p1.Cost += p2.Cost;
            p1.Total += p2.Total;
            return p1;
        }

        public void Add(ushort id)
        {
            int value = (FloorMode) ? GetFloorPrice(id) : GetPatternPrice(id);
            Cost += value;
            Total++;
        }

        public void DAdd(ushort id)
        {
            int value = (FloorMode) ? GetFloorPrice(id) : GetPatternPrice(id);
            Cost += value*2;
            Total += 2;
        }

        private static int GetPatternPrice(ushort id)
        {
            var pref = GetPatternRef(id);
            return (pref == null) ? 0 : pref.Price;
        }

        private static int GetFloorPrice(ushort id)
        {
            if (id == 1) return 0;
            var fref = GetFloorRef(id);
            return (fref == null) ? 0 : fref.Price;
        }

        private static WallReference GetPatternRef(ushort id)
        {
            WallReference result = null;
            Walls.Entries.TryGetValue(id, out result);
            return result;
        }
        private static FloorReference GetFloorRef(ushort id)
        {
            FloorReference result = null;
            Floors.Entries.TryGetValue(id, out result);
            return result;
        }
    }
}
