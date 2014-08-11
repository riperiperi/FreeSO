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
