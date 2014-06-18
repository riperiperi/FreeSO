using System;
using System.Collections.Generic;
using System.Text;
using TSOClient.Events;
using ProtocolAbstractionLibraryD;

namespace TSOClient.Network.Events
{
    public class CityTransitionEvent : EventObject
    {
        public CharacterCreationStatus CCStatus;
        public bool TransitionedToCServer = false;

        public CityTransitionEvent(EventCodes ECode)
            : base(ECode)
        {
        }
    }
}
