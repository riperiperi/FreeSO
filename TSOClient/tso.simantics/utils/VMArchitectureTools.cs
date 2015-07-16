using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.world.model;

namespace TSO.Simantics.utils
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

        internal static void DrawWall(VMArchitecture vMArchitecture, Point point, int x2, int y2, ushort pattern, ushort style, object level)
        {
            throw new NotImplementedException();
        }

        private static WallSegments AnyDiag = WallSegments.HorizontalDiag | WallSegments.VerticalDiag;

        //things 2 note
        //default style is 1
        //default pattern is 0
        //mid drawing pattern/style is 255
        public static bool DrawWall(VMArchitecture target, Point pos, int length, int direction, ushort pattern, ushort style, sbyte level)
        {
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
            return true;
        }

        public static bool EraseWall(VMArchitecture target, Point pos, int length, int direction, ushort pattern, ushort style, sbyte level)
        {
            pos += WLStartOff[direction];
            bool diagCheck = (direction % 2 == 1);
            for (int i = 0; i < length; i++)
            {
                var wall = target.GetWall((short)pos.X, (short)pos.Y, level);
                wall.Segments &= ~WLMainSeg[direction];               
                target.SetWall((short)pos.X, (short)pos.Y, level, wall);

                if (!diagCheck)
                {
                    var tPos = pos + WLSubOff[direction / 2];
                    wall = target.GetWall((short)tPos.X, (short)tPos.Y, level);
                    wall.Segments &= ~WLSubSeg[direction / 2];

                    target.SetWall((short)tPos.X, (short)tPos.Y, level, wall);
                    pos += WLStep[direction];
                }
            }
            return true;
        }

        public static bool DrawWallRect(VMArchitecture target, Rectangle rect, ushort pattern, ushort style, sbyte level)
        {
            DrawWall(target, new Point(rect.X, rect.Y), rect.Width, 0, pattern, style, level);
            DrawWall(target, new Point(rect.X, rect.Y), rect.Height, 2, pattern, style, level);
            DrawWall(target, new Point(rect.X, rect.Y + rect.Height), rect.Width, 0, pattern, style, level);
            DrawWall(target, new Point(rect.X + rect.Width, rect.Y), rect.Height, 2, pattern, style, level);
            return true;
        }
    }
}
