/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO CityServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace TSO_CityServer.Network
{
    /// <summary>
    /// A client's token + the character's GUID, as received by the LoginServer.
    /// </summary>
    public class ClientToken
    {
        public int AccountID = 0;
        public string ClientIP = "";
        public string CharacterGUID = "";
        public string Token = "";
    }
}
