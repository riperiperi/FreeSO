using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Domain.Realestate
{
    public class MapCoordinates
    {
        public static MapCoordinate Offset(ushort x, ushort y, int offsetX, int offsetY)
        {
            return Offset(new MapCoordinate(x, y), offsetX, offsetY);
        }

        public static MapCoordinate Offset(MapCoordinate coord, int offsetX, int offsetY)
        {
            //Tile above = 0, -1
            //Tile below = 0, 1
            //Tile left = -1, 0
            //Tile right = 1, 0
            return new MapCoordinate((ushort)(coord.X - offsetY), (ushort)(coord.Y + offsetX));
        }

        public static bool InBounds(ushort x, ushort y){
            return InBounds(x, y, 0);
        }

        public static bool InBounds(ushort x, ushort y, ushort padding)
        {
            if (y < padding) { return false; }
            if (y > (511 - padding)) { return false; }
            
            var xStart = 0;
            var xEnd = 0;

            if (y < 306){
                xStart = 306 - y;
            }else{
                xStart = y - 306;
            }

            if (y < 205){
                xEnd = 307 + y;
            }else{
                xEnd = 512 - (y - 205);
            }

            if (x < xStart + padding) { return false; }
            if (x > xEnd - padding) { return false; }

            return true;
        }

        public static uint Pack(ushort x, ushort y)
        {
            return (uint)(x << 16 | y);
        }

        public static MapCoordinate Unpack(uint value)
        {
            var x = value >> 16;
            var y = value & 0xFFFF;
            return new MapCoordinate((ushort)x, (ushort)y);
        }
    }

    public struct MapCoordinate
    {
        public MapCoordinate(ushort x, ushort y)
        {
            X = x;
            Y = y;
        }

        public ushort X;
        public ushort Y;

        public Vector2 ToVector2()
        {
            return new Vector2(X, Y);
        }
    }
}
