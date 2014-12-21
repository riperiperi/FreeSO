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
using TSOClient.Events;
using ProtocolAbstractionLibraryD;

namespace TSOClient.Network.Events
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
