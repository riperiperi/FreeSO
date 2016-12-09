using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.LotView.Model;
using FSO.SimAntics.Marshals;
using Microsoft.Xna.Framework;

namespace FSO.SimAntics.Utils
{
    /// <summary>
    /// Functions and utility functions to rotate a lot.
    /// </summary>
    public class VMLotRotate
    {
        private VMArchitectureMarshal arch;

        //variables for rotation transform

        private static Point[] XForward = new Point[]
        {
            new Point(1, 0), //top going to right
            new Point(0, 1), //right going to bottom
            new Point(-1, 0),
            new Point(0, -1)
        };

        private static Point[] Offset = new Point[]
        {
            new Point(0, 0), //top
            new Point(1, 0), //right (width)
            new Point(1, 1), //bottom (width,height)
            new Point(0, 1) //left (height)
        };


        public void Rotate()
        {

        }

        private int GetOffset(int x, int y)
        {
            return arch.Width * y + x;
        }

        private WallTile RotateWall(LotView.WorldRotation rot, WallTile input, short x, short y, sbyte level)
        {
            int rotN = (int)rot;
            var output = new WallTile();
            if (rot != 0)
            {
                if (input.Segments == WallSegments.HorizontalDiag)
                {
                    output.Segments = ((rotN % 2) == 0) ? WallSegments.HorizontalDiag : WallSegments.VerticalDiag;
                    output.TopRightStyle = input.TopRightStyle;
                    switch (rotN)
                    {
                        case 1:
                            output.BottomRightPattern = input.BottomLeftPattern;
                            output.BottomLeftPattern = input.BottomRightPattern;
                            output.TopLeftStyle = input.TopLeftPattern;
                            output.TopLeftPattern = input.TopLeftStyle;
                            break;
                        case 2:
                            output.BottomRightPattern = input.BottomLeftPattern; //flip sides
                            output.BottomLeftPattern = input.BottomRightPattern;
                            output.TopLeftStyle = input.TopLeftPattern;
                            output.TopLeftPattern = input.TopLeftStyle;
                            break;
                        case 3:
                            output.BottomRightPattern = input.BottomRightPattern;
                            output.BottomLeftPattern = input.BottomLeftPattern;
                            output.TopLeftStyle = input.TopLeftStyle;
                            output.TopLeftPattern = input.TopLeftPattern;
                            break;
                    }

                }
                else if (input.Segments == WallSegments.VerticalDiag)
                {
                    output.Segments = ((rotN % 2) == 0) ? WallSegments.VerticalDiag : WallSegments.HorizontalDiag;
                    output.TopRightStyle = input.TopRightStyle;
                    switch (rotN)
                    {
                        case 1:
                            output.BottomRightPattern = input.BottomRightPattern;
                            output.BottomLeftPattern = input.BottomLeftPattern;
                            output.TopLeftStyle = input.TopLeftStyle;
                            output.TopLeftPattern = input.TopLeftPattern;
                            break;
                        case 2:
                            output.BottomRightPattern = input.BottomLeftPattern; //flip sides
                            output.BottomLeftPattern = input.BottomRightPattern;
                            output.TopLeftStyle = input.TopLeftPattern;
                            output.TopLeftPattern = input.TopLeftStyle;
                            break;
                        case 3:
                            output.BottomRightPattern = input.BottomLeftPattern;
                            output.BottomLeftPattern = input.BottomRightPattern;
                            output.TopLeftStyle = input.TopLeftPattern;
                            output.TopLeftPattern = input.TopLeftStyle;
                            break;
                    }
                }
                else
                {
                    switch (rotN)
                    {
                        case 1:
                            if ((input.Segments & WallSegments.TopLeft) > 0) output.Segments |= WallSegments.TopRight;
                            if ((input.Segments & WallSegments.TopRight) > 0) output.Segments |= WallSegments.BottomRight;
                            if ((input.Segments & WallSegments.BottomRight) > 0) output.Segments |= WallSegments.BottomLeft;
                            if ((input.Segments & WallSegments.BottomLeft) > 0) output.Segments |= WallSegments.TopLeft;
                            output.TopLeftPattern = input.BottomLeftPattern;
                            output.TopRightPattern = input.TopLeftPattern;
                            output.BottomRightPattern = input.TopRightPattern;
                            output.BottomLeftPattern = input.BottomRightPattern;

                            
                            if (y + 1 < arch.Height)
                            {
                                var newLeft = arch.Walls[level - 1][GetOffset(x, y + 1)];
                                output.TopLeftStyle = newLeft.TopRightStyle;
                                output.ObjSetTLStyle = newLeft.ObjSetTRStyle;
                                output.TopLeftDoor = newLeft.TopRightDoor;
                            }

                            output.TopRightStyle = input.TopLeftStyle;
                            output.ObjSetTRStyle = input.ObjSetTLStyle;
                            output.TopRightDoor = input.TopLeftDoor;
                            break;
                        case 2:
                            if ((input.Segments & WallSegments.TopLeft) > 0) output.Segments |= WallSegments.BottomRight;
                            if ((input.Segments & WallSegments.TopRight) > 0) output.Segments |= WallSegments.BottomLeft;
                            if ((input.Segments & WallSegments.BottomRight) > 0) output.Segments |= WallSegments.TopLeft;
                            if ((input.Segments & WallSegments.BottomLeft) > 0) output.Segments |= WallSegments.TopRight;
                            output.TopLeftPattern = input.BottomRightPattern;
                            output.TopRightPattern = input.BottomLeftPattern;
                            output.BottomRightPattern = input.TopLeftPattern;
                            output.BottomLeftPattern = input.TopRightPattern;

                            if (y + 1 < arch.Height)
                            {
                                var newRight = arch.Walls[level - 1][GetOffset(x, y + 1)];
                                output.TopRightStyle = newRight.TopRightStyle;
                                output.ObjSetTRStyle = newRight.ObjSetTRStyle;
                                output.TopRightDoor = newRight.TopRightDoor;
                            }

                            if (x + 1 < arch.Width)
                            {
                                var newLeft = arch.Walls[level - 1][GetOffset(x + 1, y)];
                                output.TopLeftStyle = newLeft.TopLeftStyle;
                                output.ObjSetTLStyle = newLeft.ObjSetTLStyle;
                                output.TopLeftDoor = newLeft.TopLeftDoor;
                            }

                            break;
                        case 3:
                            if ((input.Segments & WallSegments.TopLeft) > 0) output.Segments |= WallSegments.BottomLeft;
                            if ((input.Segments & WallSegments.TopRight) > 0) output.Segments |= WallSegments.TopLeft;
                            if ((input.Segments & WallSegments.BottomRight) > 0) output.Segments |= WallSegments.TopRight;
                            if ((input.Segments & WallSegments.BottomLeft) > 0) output.Segments |= WallSegments.BottomRight;
                            output.TopLeftPattern = input.TopRightPattern;
                            output.TopRightPattern = input.BottomRightPattern;
                            output.BottomRightPattern = input.BottomLeftPattern;
                            output.BottomLeftPattern = input.TopLeftPattern;

                            output.TopLeftStyle = input.TopRightStyle;
                            output.TopLeftDoor = input.TopRightDoor;
                            output.ObjSetTLStyle = input.ObjSetTRStyle;

                            if (x + 1 < arch.Width)
                            {
                                var newRight = arch.Walls[level - 1][GetOffset(x + 1, y)];
                                output.TopRightStyle = newRight.TopLeftStyle;
                                output.ObjSetTRStyle = newRight.ObjSetTLStyle;
                                output.TopRightDoor = newRight.TopLeftDoor;
                            }
                            break;
                    }
                }
            }
            else
            {
                output = input;
            }

            return output;
        }
    }
}
