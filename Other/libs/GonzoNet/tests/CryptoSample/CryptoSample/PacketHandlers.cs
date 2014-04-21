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
        //Not sure why, but both keys must derive from the same master for decryption to work.
        private static CngKey m_MasterKey = CngKey.Create(CngAlgorithm.ECDiffieHellmanP256);
        public static ECDiffieHellmanCng ClientKey = new ECDiffieHellmanCng(m_MasterKey), 
            ServerKey = new ECDiffieHellmanCng(m_MasterKey);

        private static RNGCryptoServiceProvider m_Random = new RNGCryptoServiceProvider();
        private static byte[] ChallengeResponse;
        public static byte[] ClientNOnce;

        /// <summary>
        /// A client requested login.
        /// </summary>
        /// <param name="Client">NetworkClient instance.</param>
        /// <param name="Packet">ProcessedPacket instance.</param>
        public static void InitialClientConnect(NetworkClient Client, ProcessedPacket Packet)
        {
            Console.WriteLine("Server receives data - test 1");

            byte[] Nonce = Packet.ReadBytes(16);

            //Username would normally be used to lookup client's public key in DB (only time such a use is valid).
            string Username = Packet.ReadPascalString();
            ECDiffieHellmanPublicKey ClientPub = StaticStaticDiffieHellman.ImportKey("ClientPublic.dat");

            PacketStream EncryptedPacket = new PacketStream(0x02, 0);
            EncryptedPacket.WriteHeader();

            byte[] ClientKey = Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.Key;
            byte[] ClientIV = Client.ClientEncryptor.GetDecryptionArgsContainer().AESDecryptArgs.IV;

            ChallengeResponse = new byte[16];
            m_Random.GetNonZeroBytes(ChallengeResponse);

            MemoryStream StreamToEncrypt = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(StreamToEncrypt);
            Writer.Write((byte)ChallengeResponse.Length);
            Writer.Write(ChallengeResponse, 0, ChallengeResponse.Length);
            Writer.Write((byte)ClientKey.Length);
            Writer.Write(ClientKey, 0, ClientKey.Length);
            Writer.Write((byte)ClientIV.Length);
            Writer.Write(ClientIV, 0, ClientIV.Length);
            Writer.Flush();

            byte[] EncryptedData = StaticStaticDiffieHellman.Encrypt(ServerKey, ClientPub, Nonce, 
                StreamToEncrypt.ToArray());

            EncryptedPacket.WriteUInt16((ushort)(PacketHeaders.UNENCRYPTED + 1 + EncryptedData.Length));

            EncryptedPacket.WriteByte((byte)EncryptedData.Length);
            EncryptedPacket.Write(EncryptedData, 0, EncryptedData.Length);

            Client.Send(EncryptedPacket.ToArray());

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

            byte[] PacketBuf = new byte[Packet.ReadByte()];
            Packet.Read(PacketBuf, 0, (int)PacketBuf.Length);

            ECDiffieHellmanPublicKey ServerPub = StaticStaticDiffieHellman.ImportKey("ServerPublic.dat");

            MemoryStream DecryptedStream = new MemoryStream(StaticStaticDiffieHellman.Decrypt(ClientKey, ServerPub, 
                ClientNOnce, PacketBuf));
            BinaryReader Reader = new BinaryReader(DecryptedStream);

            byte[] ChallengeResponse = Reader.ReadBytes(Reader.ReadByte());

            //Yay, we have key and IV, we can now start encryption with AES!
            Client.ClientEncryptor = new AESEncryptor(Reader.ReadBytes(Reader.ReadByte()), Reader.ReadBytes(Reader.ReadByte()), "");

            MemoryStream StreamToEncrypt = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(StreamToEncrypt);
            Writer.Write((byte)ChallengeResponse.Length);
            Writer.Write(ChallengeResponse, 0, ChallengeResponse.Length);

            //Encrypt data using key and IV from server, hoping that it'll be decrypted correctly at the other end...
            Client.SendEncrypted(0x03, StreamToEncrypt.ToArray());

            Console.WriteLine("Test 2: passed!");
        }

        public static void HandleChallengeResponse(NetworkClient Client, ProcessedPacket Packet)
        {
            Console.WriteLine("Server receives challenge response - test 3");

            byte[] CResponse = Packet.ReadBytes(Packet.ReadByte());

            if (CResponse.SequenceEqual(ChallengeResponse))
                Console.WriteLine("Received correct challenge response, client was authenticated!");

            Console.WriteLine("Test 3: passed!");
        }
    }
}
