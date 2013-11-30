using System;
using System.Collections.Generic;
using System.Text;
using TSOClient.Events;

namespace TSOClient.Network.Events
{
    public class CityTransitionEvent : EventObject
    {
        public bool Success;

        public CityTransitionEvent(EventCodes ECode)
            : base(ECode)
        {
        }
    }
}
