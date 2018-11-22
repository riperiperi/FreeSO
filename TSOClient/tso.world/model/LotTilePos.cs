/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using System.IO;

namespace FSO.LotView.Model
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

        public int TileID
        {
            get
            {
                return (int)TileX | ((int)TileY << 8) | ((int)Level << 16);
            }
        }

        public static LotTilePos FromBigTile(short x, short y, sbyte level)
        {
            return new LotTilePos((short)((x << 4) + 8), (short)((y << 4) + 8), level);
        }

        //TODO: below operations can desync if float behaviour is different.
        public static LotTilePos FromVec3(Vector3 pos)
        {
            return new LotTilePos((short)Math.Round(pos.X * 16), (short)Math.Round(pos.Y * 16), (sbyte)(Math.Round(pos.Z / 2.95) + 1));
        }

        public static LotTilePos FromVec2(Vector2 pos)
        {
            return new LotTilePos((short)Math.Round(pos.X), (short)Math.Round(pos.Y), 0);
        }

        public static int Distance(LotTilePos a, LotTilePos b)
        {
            return (int)Math.Sqrt((a.x - b.x) * (a.x - b.x) + (a.y - b.y) * (a.y - b.y)); 
            //TODO: consider level? does anything need this?
        }

        public static LotTilePos operator +(LotTilePos c1, LotTilePos c2) //use for offsets ONLY!
        {
            return new LotTilePos((short)(c1.x+c2.x), (short)(c1.y+c2.y), (sbyte)(c1.Level+c2.Level));
        }

        public static LotTilePos operator *(LotTilePos c1, int c2) //use for offsets ONLY!
        {
            return new LotTilePos((short)(c1.x * c2), (short)(c1.y * c2), (sbyte)(c1.Level * c2));
        }

        public static LotTilePos operator -(LotTilePos c1, LotTilePos c2) //use for offsets ONLY!
        {
            return new LotTilePos((short)(c1.x - c2.x), (short)(c1.y - c2.y), (sbyte)(c1.Level - c2.Level));
        }

        public static LotTilePos operator /(LotTilePos c1, int div) //use for offsets ONLY!
        {
            return new LotTilePos((short)(c1.x/div), (short)(c1.y/div), (sbyte)(c1.Level/div));
        }

        public static bool operator ==(LotTilePos c1, LotTilePos c2) //are these necessary?
        {
            return equals(c1, c2);
        }

        public static bool operator !=(LotTilePos c1, LotTilePos c2)
        {
            return !equals(c1, c2);
        }

        private static bool equals(LotTilePos c1, LotTilePos c2)
        {
            return c1.x == c2.x && c1.y == c2.y && c1.Level == c2.Level;
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

        public Vector3 ToVector3()
        {
            return new Vector3(x / 16f, y / 16f, (Level - 1) * 2.95f);
        }

        public Point ToPoint()
        {
            return new Point(x, y);
        }

        public static LotTilePos OUT_OF_WORLD = new LotTilePos(-32768, -32768, 1);

        public void Deserialize(BinaryReader reader)
        {
            x = reader.ReadInt16();
            y = reader.ReadInt16();
            Level = reader.ReadSByte();
        }

        public void SerializeInto(BinaryWriter writer)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(Level);
        }
    }
}
