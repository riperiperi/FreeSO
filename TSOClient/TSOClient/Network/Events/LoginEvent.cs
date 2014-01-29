using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Events;

namespace TSOClient.Network.Events
{
    public class LoginEvent : EventObject
    {
        public bool Success;
        public bool VersionOK;

        public LoginEvent(EventCodes ECode)
            : base(ECode)
        {
        }
    }
}
