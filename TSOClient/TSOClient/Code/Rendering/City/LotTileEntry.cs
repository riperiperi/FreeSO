/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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
