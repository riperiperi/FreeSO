/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace FSO.Client.Network.Events
{
    /// <summary>
    /// Base class for all network events.
    /// </summary>
    public class NetworkEvent : EventObject
    {
        public NetworkEvent(EventCodes ECode)
            : base(ECode)
        {

        }
    }
}
