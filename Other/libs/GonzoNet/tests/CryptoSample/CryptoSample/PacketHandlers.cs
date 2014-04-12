using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
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

        private static AESEncryptor m_Encryptor;
        private static Guid ChallengeResponse = Guid.NewGuid();
        public static Guid ClientNOnce = Guid.NewGuid();

        /// <summary>
        /// A client requested login.
        /// </summary>
        /// <param name="Client">NetworkClient instance.</param>
        /// <param name="Packet">ProcessedPacket instance.</param>
        public static void InitialClientConnect(NetworkClient Client, ProcessedPacket Packet)
        {
            Console.WriteLine("Server receives data - test 1");

            //AES is used to encrypt all further communication between client and server.
            AesCryptoServiceProvider AesCrypto = new AesCryptoServiceProvider();
            AesCrypto.GenerateKey();
            AesCrypto.GenerateIV();
            m_Encryptor = new AESEncryptor(AesCrypto.Key, AesCrypto.IV, "");

            Guid Nonce = new Guid(Packet.ReadPascalString());

            //Username would normally be used to lookup client's public key in DB (only time such a use is valid).
            string Username = Packet.ReadPascalString();
            ECDiffieHellmanPublicKey ClientPub = StaticStaticDiffieHellman.ImportKey("ClientPublic.dat");

            PacketStream EncryptedPacket = new PacketStream(0x02, 0);
            EncryptedPacket.WriteHeader();

            MemoryStream StreamToEncrypt = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(StreamToEncrypt);
            Writer.Write((byte)ChallengeResponse.ToByteArray().Length);
            Writer.Write(ChallengeResponse.ToByteArray(), 0, ChallengeResponse.ToByteArray().Length);
            Writer.Write((byte)m_Encryptor.Key.Length);
            Writer.Write(m_Encryptor.Key, 0, m_Encryptor.Key.Length);
            Writer.Write((byte)m_Encryptor.IV.Length);
            Writer.Write(m_Encryptor.IV, 0, m_Encryptor.IV.Length);
            Writer.Flush();

            byte[] EncryptedData = StaticStaticDiffieHellman.Encrypt(ServerKey, ClientPub, Nonce.ToByteArray(), 
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
                ClientNOnce.ToByteArray(), PacketBuf));
            BinaryReader Reader = new BinaryReader(DecryptedStream);

            Guid ChallengeResponse = new Guid(Reader.ReadBytes(Reader.ReadByte()));
            m_Encryptor = new AESEncryptor(Reader.ReadBytes(Reader.ReadByte()), Reader.ReadBytes(Reader.ReadByte()), "");

            //Yay, we have key and IV, we can now start encryption with AES!
            AES AesEncryptor = new AES(m_Encryptor.Key, m_Encryptor.IV);

            PacketStream EncryptedPacket = new PacketStream(0x03, 0);
            EncryptedPacket.WriteHeader();

            MemoryStream StreamToEncrypt = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(StreamToEncrypt);
            Writer.Write((byte)ChallengeResponse.ToByteArray().Length);
            Writer.Write(ChallengeResponse.ToByteArray(), 0, ChallengeResponse.ToByteArray().Length);

            //Encrypt data using key and IV from server, hoping that it'll be decrypted correctly at the other end...
            byte[] EncryptedData = AesEncryptor.Encrypt(StreamToEncrypt.ToArray());

            EncryptedPacket.WriteUInt16((ushort)(PacketHeaders.UNENCRYPTED + EncryptedData.Length + 1));
            EncryptedPacket.WriteByte((byte)EncryptedData.Length);
            EncryptedPacket.Write(EncryptedData, 0, EncryptedData.Length);

            Client.Send(EncryptedPacket.ToArray());

            Console.WriteLine("Test 2: passed!");
        }

        public static void HandleChallengeResponse(NetworkClient Client, ProcessedPacket Packet)
        {
            Console.WriteLine("Server receives challenge response - test 3");

            byte[] PacketBuf = new byte[Packet.ReadByte()];
            Packet.Read(PacketBuf, 0, (int)PacketBuf.Length);

            AES AesEncryptor = new AES(m_Encryptor.Key, m_Encryptor.IV);
            MemoryStream DecryptedStream = new MemoryStream(AesEncryptor.Decrypt(PacketBuf));
            BinaryReader Reader = new BinaryReader(DecryptedStream);

            byte[] CResponseBuf = Reader.ReadBytes(Reader.ReadByte());
            Guid CResponse = new Guid(CResponseBuf);

            if (CResponse.CompareTo(ChallengeResponse) == 0)
                Console.WriteLine("Received correct challenge response, client was authenticated!");

            Console.WriteLine("Test 3: passed!");
        }
    }
}
