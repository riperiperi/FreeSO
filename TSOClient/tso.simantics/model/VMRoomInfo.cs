/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Model
{
    public struct VMRoomInfo
    {
        public List<VMRoomPortal> Portals;
    }

    public class VMRoomPortal {
        public short ObjectID;
        public ushort TargetRoom;

        public VMRoomPortal(short obj, ushort target)
        {
            ObjectID = obj;
            TargetRoom = target;
        }
    }
}
