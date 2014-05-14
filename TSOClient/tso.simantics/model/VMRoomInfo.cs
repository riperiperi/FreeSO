using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSO.Simantics.model
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
