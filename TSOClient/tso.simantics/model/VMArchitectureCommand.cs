using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Simantics.model
{
    public struct VMArchitectureCommand
    {
        public VMArchitectureCommandType Type;
        public int x;
        public int y;
        public sbyte level;

        public int x2; //for RECT: width and height. for LINE: length and direction. Not important for fill.
        public int y2;

        public ushort pattern;
        public ushort style; //for walls, obvious. maybe means something else for floors on diagonals

    }

    public enum VMArchitectureCommandType
    {
        WALL_LINE,
        WALL_RECT,

        PATTERN_RECT,
        PATTERN_FILL,

        FLOOR_RECT,
        FLOOR_FILL
    }
}
