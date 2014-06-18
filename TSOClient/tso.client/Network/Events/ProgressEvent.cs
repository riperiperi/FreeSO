using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Events;

namespace TSOClient.Network.Events
{
    public class ProgressEvent : EventObject
    {
        public int Done;
        public int Total;

        public ProgressEvent(EventCodes ECode)
            : base(ECode)
        {
        }
    }
}
