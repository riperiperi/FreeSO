/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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

        PACKET_PROCESSING_ERROR = 0x05 //Received a faulty packet that couldn't be processed.
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
