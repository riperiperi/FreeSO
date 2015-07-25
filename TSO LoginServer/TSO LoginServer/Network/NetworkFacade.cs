using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography;
using GonzoNet;

namespace TSO_LoginServer.Network
{
    class NetworkFacade
    {
        public static CityServerListener CServerListener;
        public static Listener ClientListener;

		public const int INITIAL_MONEY = 5000; //Hardcoded for now...

        public static ECDiffieHellmanCng ServerKey = new ECDiffieHellmanCng();
        public static byte[] ServerPublicKey = ServerKey.PublicKey.ToByteArray();
    }
}
