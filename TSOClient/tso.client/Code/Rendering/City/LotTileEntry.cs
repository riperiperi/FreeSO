/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Rhys Simpson. All Rights Reserved.

Contributor(s): ______________________________________.
*/

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
        public byte flags; //bit 0 = online, bit 1 = spotlight, bit 2 = locked, bit 3 = occupied, other bits free for whatever use
        public int cost;
        public string name;

        public LotTileEntry(int Lotid, string Name, short X, short Y,  byte Flags, int Cost)
        {
            this.lotid = Lotid;
            this.x = X;
            this.y = Y;
            this.flags = Flags;
            this.cost = Cost;
            this.name = Name;
        }
    }
}
