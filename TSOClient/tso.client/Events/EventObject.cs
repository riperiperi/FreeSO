/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace TSOClient.Events
{
    /// <summary>
    /// Codes for various events that can occur.
    /// </summary>
    public enum EventCodes
    {
        BAD_USERNAME = 0x00,
        BAD_PASSWORD = 0x01,

        LOGIN_RESULT = 0x02,
        PROGRESS_UPDATE = 0x03,
        TRANSITION_RESULT = 0x04,

        PACKET_PROCESSING_ERROR = 0x05, //Received a faulty packet that couldn't be processed.
        AUTHENTICATION_FAILURE = 0x06
    }

    /// <summary>
    /// Base class for all events.
    /// </summary>
    public class EventObject
    {
        public EventCodes ECode;

        public EventObject(EventCodes Code)
        {
            ECode = Code;
        }
    }
}
