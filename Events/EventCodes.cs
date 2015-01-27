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

namespace GonzoNet.Events
{
    public enum EventCodes
    {
        BAD_USERNAME = 0x00,
        BAD_PASSWORD = 0x01,

        LOGIN_RESULT = 0x02,
        PROGRESS_UPDATE = 0x03,

        PACKET_PROCESSING_ERROR = 0x04, //Received a faulty packet that couldn't be processed.
        PACKET_DECRYPTION_ERROR = 0x05
    }
}
