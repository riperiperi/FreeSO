/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the GonzoNet.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using GonzoNet.Encryption;

namespace GonzoNet
{
    /// <summary>
    /// Container for arguments supplied when logging in,
    /// to the OnConnected delegate in NetworkClient.cs.
    /// This acts as a base class that can be inherited
    /// from to accommodate more/different arguments.
    /// </summary>
    public class LoginArgsContainer
    {
        //This can be used by a handler to send packets when connected.
        public NetworkClient Client;
        //Encryptor instance used to en/decrypt the packets received.
        public Encryptor Enc;
        public string Username;
        public string Password;
    }
}
