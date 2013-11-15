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
        public NetworkClient Connection;
        public Encryptor Enc;
        public string Username;
        public string Password;
    }
}
