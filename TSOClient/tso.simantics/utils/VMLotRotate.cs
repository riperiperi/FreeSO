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
        private VMArchitectureMarshal Arch;
        private VMEntityMarshal[] Ents;

        //variables for rotation transform

        public VMLotRotate(VMMarshal marshal)
        {
            Arch = marshal.Context.Architecture;
            Ents = marshal.Entities;
        }

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

        public void Rotate(int notches)
        {
            if (notches == 0) return;

            var newXDir = XForward[notches];
            var newYDir = XForward[(notches + 1) % 4];
            var offset = new Point(Offset[notches].X * (Arch.Width - 2), Offset[notches].Y * (Arch.Height - 2));
            for (int i = 0; i < Arch.Stories; i++) {
                int index = 0;
                var walls = Arch.Walls[i];
                var floors = Arch.Floors[i];
                var newWalls = new WallTile[walls.Length];
                var newFloors = new FloorTile[floors.Length];
                for (int y = 0; y < Arch.Height; y++)
                {
                    for (int x = 0; x < Arch.Width; x++)
                    {
                        var newX = x * newXDir.X + y * newYDir.X + offset.X;
                        var newY = x * newXDir.Y + y * newYDir.Y + offset.Y;
                        if (newX < 0 || newY < 0)
                        {
                            index++;
                            continue;
                        }
                        var newIndex = newY * Arch.Width + newX;
                        newWalls[newIndex] = RotateWall(notches, walls[index], (short)x, (short)y, (sbyte)(i + 1));
                        newFloors[newIndex] = floors[index];
                        index++;
                    }
                }
                Arch.Walls[i] = newWalls;
                Arch.Floors[i] = newFloors;
            }

            offset = new Point(Offset[notches].X * ((Arch.Width-1) * 16), Offset[notches].Y * ((Arch.Height-1) * 16));

            for (int i=0; i < Ents.Length; i++)
            {
                var oldPos = Ents[i].Position;
                if (oldPos != LotTilePos.OUT_OF_WORLD)
                {
                    Ents[i].Position = new LotTilePos(
                        (short)(oldPos.x * newXDir.X + oldPos.y * newYDir.X + offset.X),
                        (short)(oldPos.x * newXDir.Y + oldPos.y * newYDir.Y + offset.Y),
                        oldPos.Level);
                }
                if (Ents[i] is VMGameObjectMarshal)
                {
                    var m = (VMGameObjectMarshal)Ents[i];
                    m.Direction = RotateDirection(m.Direction, notches);
                }
            }
        }

        internal Direction RotateDirection(Direction ws, int rotate)
        {
            int dir = (int)ws;
            int rotPart = ((dir << rotate*2) & 255) | ((dir & 255) >> (8 - rotate * 2));
            return (Direction)rotPart;
        }

        private int GetOffset(int x, int y)
        {
            return Arch.Width * y + x;
        }

        private WallTile RotateWall(int rotN, WallTile input, short x, short y, sbyte level)
        {
            var output = new WallTile();
            if (rotN != 0)
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

                            
                            if (y + 1 < Arch.Height)
                            {
                                var newLeft = Arch.Walls[level - 1][GetOffset(x, y + 1)];
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

                            if (y + 1 < Arch.Height)
                            {
                                var newRight = Arch.Walls[level - 1][GetOffset(x, y + 1)];
                                output.TopRightStyle = newRight.TopRightStyle;
                                output.ObjSetTRStyle = newRight.ObjSetTRStyle;
                                output.TopRightDoor = newRight.TopRightDoor;
                            }

                            if (x + 1 < Arch.Width)
                            {
                                var newLeft = Arch.Walls[level - 1][GetOffset(x + 1, y)];
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

                            if (x + 1 < Arch.Width)
                            {
                                var newRight = Arch.Walls[level - 1][GetOffset(x + 1, y)];
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
