/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using FSO.SimAntics.Model.Routing;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.SimAntics.Model
{
    public struct VMRoomInfo
    {
        public List<VMRoomPortal> Portals;
        public List<VMEntity> Entities;
        public LotRoom Room;
    }

    public struct LotRoom
    {
        public ushort RoomID;
        public ushort AmbientLight;
        public bool IsOutside;
        public ushort Area;
        public bool IsPool;
        public bool Unroutable;

        public List<VMObstacle> WallObs;
        public List<VMObstacle> RoomObj;
        public Rectangle Bounds;
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
