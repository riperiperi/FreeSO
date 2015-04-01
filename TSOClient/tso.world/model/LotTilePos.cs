using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace tso.world.model
{
    public struct LotTilePos
    {
        public short x;
        public short y;
        public sbyte Level;

        public LotTilePos(short x, short y, sbyte level)
        {
            this.x = x; this.y = y; Level = level;
        }

        public static LotTilePos FromBigTile(short x, short y, sbyte level)
        {
            return new LotTilePos((short)((x << 4) + 8), (short)((y << 4) + 8), level);
        }

        public static LotTilePos operator +(LotTilePos c1, LotTilePos c2) //use for offsets ONLY!
        {
            return new LotTilePos((short)(c1.x+c2.x), (short)(c1.y+c2.y), (sbyte)(c1.Level+c2.Level));
        }

        public static LotTilePos operator -(LotTilePos c1, LotTilePos c2) //use for offsets ONLY!
        {
            return new LotTilePos((short)(c1.x - c2.x), (short)(c1.y - c2.y), (sbyte)(c1.Level - c2.Level));
        }

        public LotTilePos(LotTilePos pos) {
            x = pos.x;
            y = pos.y;
            Level = pos.Level;
        }

        public short TileX
        {
            get
            {
                return (short)(x >> 4);
            }
            set
            {
                x = (short)(value << 4);
            }
        }

        public short TileY
        {
            get
            {
                return (short)(y >> 4);
            }
            set
            {
                y = (short)(value << 4);
            }
        }
    }
}
