using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Events
{
    public enum EventCodes
    {
        BAD_USERNAME = 0x00,
        BAD_PASSWORD = 0x01,

        LOGIN_RESULT = 0x02,
        PROGRESS_UPDATE = 0x03,
        TRANSITION_RESULT = 0x04,

        PACKET_PROCESSING_ERROR = 0x05 //Received a faulty packet that couldn't be processed.
    }

    public class EventObject
    {
        public EventCodes ECode;

        public EventObject(EventCodes Code)
        {
            ECode = Code;
        }
    }
}
