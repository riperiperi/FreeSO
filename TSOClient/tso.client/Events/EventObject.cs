using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Events
{
    enum EventCodes
    {
        BAD_USERNAME = 0x00,
        BAD_PASSWORD = 0x01
    }

    class EventObject
    {
        public EventCodes ECode;

        public EventObject(EventCodes Code)
        {
            ECode = Code;
        }
    }
}
