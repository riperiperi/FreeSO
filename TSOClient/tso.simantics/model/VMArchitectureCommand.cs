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

        //note: for pattern dot x2 is "side". 0-5, 0-3 for normal walls and 4-5 for diagonal sides

        public ushort pattern;
        public ushort style; //for walls, obvious. maybe means something else for floors on diagonals
        //style does not mean anything for pattern mode

    }

    public enum VMArchitectureCommandType
    {
        WALL_LINE,
        WALL_DELETE,
        WALL_RECT,

        PATTERN_DOT,
        PATTERN_FILL,

        FLOOR_RECT,
        FLOOR_FILL
    }
}
