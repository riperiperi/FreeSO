/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.Network.Events
{
    /// <summary>
    /// Event that occurs when client progresses through a loading/login stage.
    /// </summary>
    public class ProgressEvent : EventObject
    {
        public int Done;
        public int Total;

        public ProgressEvent(EventCodes ECode)
            : base(ECode)
        {
        }
    }
}
