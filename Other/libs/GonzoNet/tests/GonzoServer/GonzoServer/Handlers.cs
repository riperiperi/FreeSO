using System;
using System.Collections.Generic;
using System.Text;
using GonzoNet;
using GonzoNet.Encryption;

namespace GonzoServer
{
    class Handlers
    {
        public static void ReceivedUnEncryptedPacket(NetworkClient Client, PacketStream Packet)
        {
            //NOTE: Normally, the client would only send its username, and that would be
            //      used to lookup the password from the database.
            string Password = Packet.ReadPascalString();
            Console.WriteLine("Received password from client: " + Password);
            
            //TODO: Client should be passed by ref?
            //ClientEncryptor must be initialized in order to decrypt encrypted messages!
            Client.ClientEncryptor = new ARC4Encryptor(Password);
        }

        public static void ReceivedEncryptedPacket(NetworkClient Client, PacketStream Packet)
        {
            Console.WriteLine("Received encrypted message: " + Packet.ReadPascalString());
        }
    }
}
