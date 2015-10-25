using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Domain.Realestate
{
    public class MapCoordinates
    {
        public static MapCoordinate Offset(MapCoordinate coord, int offsetX, int offsetY)
        {
            //Tile above = 0, -1
            //Tile below = 0, 1
            //Tile left = -1, 0
            //Tile right = 1, 0
            return new MapCoordinate((ushort)(coord.X - offsetY), (ushort)(coord.Y + offsetX));
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
    }
}
