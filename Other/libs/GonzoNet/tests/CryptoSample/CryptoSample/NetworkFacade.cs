using System;
using System.Collections.Generic;
using System.Text;
using GonzoNet;
using GonzoNet.Encryption;

namespace CryptoSample
{
    public class NetworkFacade
    {
        public static Listener Listener = new Listener(EncryptionMode.AESCrypto);
        public static NetworkClient Client;
    }
}
