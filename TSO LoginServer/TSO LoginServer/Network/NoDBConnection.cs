/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace TSO_LoginServer.Network
{
    /// <summary>
    /// Exception thrown if the server couldn't connect to an MS SQL server.
    /// </summary>
    class NoDBConnection : Exception
    {
        public NoDBConnection(string Message)
            : base(Message)
        {

        }
    }
}
