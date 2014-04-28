using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Security.Cryptography;
using GonzoNet.Encryption;

namespace Sauron
{
    class Program
    {
        static void Main(string[] args)
        {
            ECDiffieHellmanCng ServerKey = new ECDiffieHellmanCng(CngKey.Create(CngAlgorithm.ECDiffieHellmanP256, null, 
                new CngKeyCreationParameters { ExportPolicy = CngExportPolicies.AllowPlaintextExport }));

            Console.WriteLine("Creating one key to rule them all,\none key to find them,\none key to bring them all\n" + 
            "and on the interwebs bind them...\n");

            StaticStaticDiffieHellman.ExportKey("ServerPublicKey.dat", ServerKey.PublicKey);
            StaticStaticDiffieHellman.ExportKey("ServerPrivateKey.dat", ServerKey.Key);

            Console.ReadKey();
        }
    }
}
