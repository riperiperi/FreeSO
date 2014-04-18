using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GonzoNet.Encryption
{
    /// <summary>
    /// Mode of encryption to use for a connection (see Listener.cs)
    /// </summary>
    public enum EncryptionMode
    {
        NoEncryption = 0x00,
        AESCrypto = 0x01
    }
}
