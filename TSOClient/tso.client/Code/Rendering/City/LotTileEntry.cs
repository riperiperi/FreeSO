using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Code.Rendering.City
{
    public class LotTileEntry
    {
        public int lotid;
        public short x;
        public short y;
        public byte flags; //bit 0 = online, bit 1 = spotlight, bit 2 = locked, other bits free for whatever use

        public LotTileEntry(int lotid, short x, short y, byte flags)
        {
            this.lotid = lotid;
            this.x = x;
            this.y = y;
            this.flags = flags;
        }
    }
}
