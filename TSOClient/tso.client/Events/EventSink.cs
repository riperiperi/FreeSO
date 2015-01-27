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
    /// Sink to keep track of all events.
    /// </summary>
    public class EventSink
    {
        public static List<EventObject> EventQueue = new List<EventObject>();

        public static void RegisterEvent(EventObject Event)
        {
            EventQueue.Add(Event);
        }
    }
}
