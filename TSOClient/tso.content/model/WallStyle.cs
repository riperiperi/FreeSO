using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Files.formats.iff.chunks;

namespace TSO.Content.model
{
    public class WallStyle
    {
        public ushort ID;
        public SPR WallsUpNear;
        public SPR WallsUpMedium;
        public SPR WallsUpFar;
        //for most fences, the following will be null. This means to use the ones above when walls are down.
        public SPR WallsDownNear;
        public SPR WallsDownMedium;
        public SPR WallsDownFar;
    }
}
