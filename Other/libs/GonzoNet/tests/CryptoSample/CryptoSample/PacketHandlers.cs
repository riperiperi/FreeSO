using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using GonzoNet;
using GonzoNet.Encryption;

namespace CryptoSample
{
    public class PacketHandlers
    {
        public static byte[] ClientPublicKey, ServerPublicKey;
        public static ECDiffieHellmanCng ServerPrivateKey = new ECDiffieHellmanCng();

        private static RNGCryptoServiceProvider m_Random = new RNGCryptoServiceProvider();
        //Sent from server to client to verify the client.
        private static byte[] ChallengeResponse;

        //Client's password hashed, using username as salt.
        public static byte[] PasswordHash;

        /// <summary>
        /// A client requested login.
        /// </summary>
        /// <param name="Client">NetworkClient instance.</param>
        /// <param name="Packet">ProcessedPacket instance.</param>
        public static void InitialClientConnect(NetworkClient Client, ProcessedPacket Packet)
        {
            Console.WriteLine("Server receives data - test 1");

            PacketStream EncryptedPacket = new PacketStream(0x02, 0);
            EncryptedPacket.WriteHeader();

            ClientPublicKey = Packet.ReadBytes((Packet.ReadByte()));

            AESEncryptor Enc = (AESEncryptor)Client.ClientEncryptor;
            Enc.NOnce = Packet.ReadBytes((Packet.ReadByte()));
            Enc.PublicKey = ClientPublicKey;
            Enc.PrivateKey = ServerPrivateKey;
            Client.ClientEncryptor = Enc;

            //THIS IS IMPORTANT - public key must be derived from private!
            ServerPublicKey = ServerPrivateKey.PublicKey.ToByteArray();

            ChallengeResponse = new byte[16];
            m_Random.GetNonZeroBytes(ChallengeResponse);

            MemoryStream StreamToEncrypt = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(StreamToEncrypt);
            Writer.Write((byte)ChallengeResponse.Length);
            Writer.Write(ChallengeResponse, 0, ChallengeResponse.Length);
            Writer.Flush();

            byte[] EncryptedData = StaticStaticDiffieHellman.Encrypt(ServerPrivateKey,
                ECDiffieHellmanCngPublicKey.FromByteArray(ClientPublicKey, CngKeyBlobFormat.EccPublicBlob), Enc.NOnce, 
                StreamToEncrypt.ToArray());

            EncryptedPacket.WriteUInt16((ushort)(PacketHeaders.UNENCRYPTED + 
                (1 + ServerPublicKey.Length) + 
                (1 + EncryptedData.Length)));

            EncryptedPacket.WriteByte((byte)ServerPublicKey.Length);
            EncryptedPacket.WriteBytes(ServerPublicKey);
            EncryptedPacket.WriteByte((byte)EncryptedData.Length);
            EncryptedPacket.WriteBytes(EncryptedData);

            Client.Send(EncryptedPacket.ToArray());

            NetworkFacade.Listener.UpdateClient(Client);

            Console.WriteLine("Test 1: passed!");
        }

        /// <summary>
        /// Initial response from server to client.
        /// </summary>
        /// <param name="Client">A NetworkClient instance.</param>
        /// <param name="Packet">A ProcessedPacket instance.</param>
        public static void HandleServerChallenge(NetworkClient Client, ProcessedPacket Packet)
        {
            Console.WriteLine("Client receives encrypted data - test 2");

            ServerPublicKey = Packet.ReadBytes(Packet.ReadByte());
            byte[] EncryptedData = Packet.ReadBytes(Packet.ReadByte());

            AESEncryptor Enc = (AESEncryptor)Client.ClientEncryptor;
            Enc.PublicKey = ServerPublicKey;
            Client.ClientEncryptor = Enc;
            NetworkFacade.Client.ClientEncryptor = Enc;

            ECDiffieHellmanCng PrivateKey = Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.PrivateKey;
            byte[] NOnce = Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.NOnce;

            byte[] ChallengeResponse = StaticStaticDiffieHellman.Decrypt(PrivateKey,
                ECDiffieHellmanCngPublicKey.FromByteArray(ServerPublicKey, CngKeyBlobFormat.EccPublicBlob),
                NOnce, EncryptedData);

            MemoryStream StreamToEncrypt = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(StreamToEncrypt);
            
            Writer.Write((byte)ChallengeResponse.Length);
            Writer.Write(ChallengeResponse, 0, ChallengeResponse.Length);
            
            Writer.Write(Client.ClientEncryptor.Username);
            Writer.Write((byte)PasswordHash.Length);
            Writer.Write(PasswordHash);
            Writer.Flush();

            Client.SendEncrypted(0x03, StreamToEncrypt.ToArray());

            Console.WriteLine("Test 2: passed!");
        }

        public static void HandleChallengeResponse(NetworkClient Client, ProcessedPacket Packet)
        {
            Console.WriteLine("Server receives challenge response - test 3");

            byte[] CResponse = Packet.ReadBytes(Packet.ReadByte());

            if (CResponse.SequenceEqual(ChallengeResponse))
                Console.WriteLine("Received correct challenge response, client was authenticated!");

            string Username = Packet.ReadString();
            Console.WriteLine("Username: " + Username);

            byte[] PasswordHash = Packet.ReadBytes(Packet.ReadByte());

            Console.WriteLine("Test 3: passed!");
        }
    }
}
