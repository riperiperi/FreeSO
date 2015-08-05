/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text;
using ProtocolAbstractionLibraryD;

namespace FSO.Client.Network.Events
{
    /// <summary>
    /// Event that occurs when client transitions to city screen.
    /// </summary>
    public class CityTransitionEvent : EventObject
    {
        public CharacterCreationStatus CCStatus;
        public bool TransitionedToCServer = false;

        public CityTransitionEvent(EventCodes ECode)
            : base(ECode)
        {
        }
    }
}
