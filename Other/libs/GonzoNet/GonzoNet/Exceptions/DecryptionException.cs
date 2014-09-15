using System;
using System.Collections.Generic;
using System.Text;
using GonzoNet.Events;

namespace GonzoNet.Exceptions
{
    public class DecryptionException : Exception
    {
        public EventCodes ErrorCode = EventCodes.PACKET_DECRYPTION_ERROR;

        public DecryptionException(string Description)
            : base(Description)
        {

        }
    }
}
