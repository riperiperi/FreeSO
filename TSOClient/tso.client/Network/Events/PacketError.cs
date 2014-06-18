using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Events;

namespace TSOClient.Network.Events
{
    public class PacketError : EventObject
    {
        public PacketError(EventCodes ECode)
            : base(ECode)
        {
        }
    }
}
