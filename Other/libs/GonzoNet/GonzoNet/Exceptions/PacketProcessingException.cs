/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the GonzoNet.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s):
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GonzoNet.Events;

namespace GonzoNet.Exceptions
{
    /// <summary>
    /// Thrown when a packet couldn't be processed by ProcessedPacket.
    /// </summary>
    public class PacketProcessingException : Exception
    {
        public EventCodes ErrorCode = EventCodes.PACKET_PROCESSING_ERROR;

        public PacketProcessingException(string Description) : base(Description)
        {

        }
    }
}
