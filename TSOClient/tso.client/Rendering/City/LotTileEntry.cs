/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.Rendering.City
{
    public class LotTileEntry
    {
        public int lotid;
        public int packed_pos
        {
            get
            {
                return ((x << 16) | y);
            }
        }
        public short x;
        public short y;
        public LotTileFlags flags; //bit 0 = online, bit 1 = spotlight, bit 2 = locked, bit 3 = occupied, other bits free for whatever use

        public LotTileEntry(int Lotid, short X, short Y, LotTileFlags Flags)
        {
            this.lotid = Lotid;
            this.x = X;
            this.y = Y;
            this.flags = Flags;
        }

        public static LotTileEntry[] GenFromCity(Common.DataService.Model.City city)
        {
            var entries = new Dictionary<uint, LotTileEntry>();
            foreach (var property in city.City_ReservedLotInfo)
            {
                entries[property.Key] = new LotTileEntry((int)property.Key, (short)(property.Key >> 16), (short)(property.Key & 0xFFFF), property.Value?LotTileFlags.Online:0);
            }

            foreach (var spot in city.City_SpotlightsVector)
            {
                LotTileEntry entry = null;
                if (entries.TryGetValue(spot, out entry))
                    entry.flags |= LotTileFlags.Spotlight;

            }

            return entries.Values.ToArray();
        }
    }

    [Flags]
    public enum LotTileFlags
    {
        Online = 0x1,
        Spotlight = 0x2,
        Locked = 0x4,
        Occupied = 0x8
    }
}
