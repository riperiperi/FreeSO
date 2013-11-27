/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the GonzoNet.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TSOClient.Network
{
    public enum PacketType
    {
        LOGIN_REQUEST = 0x00,
        LOGIN_NOTIFY = 0x01,
        LOGIN_FAILURE = 0x02,
        CHARACTER_LIST = 0x05,
        CITY_LIST = 0x06,
        CHARACTER_CREATE = 0x07
    }
}
