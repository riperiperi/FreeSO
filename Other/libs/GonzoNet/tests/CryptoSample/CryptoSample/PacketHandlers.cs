using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using GonzoNet;

namespace CryptoSample
{
    public class PacketHandlers
    {
        //Private keys are stored by their respected entities.
        private static CngKey ClientPrivateKey = CngKey.Create(CngAlgorithm.ECDiffieHellmanP256);
        private static CngKey ServerPrivateKey = CngKey.Create(CngAlgorithm.ECDiffieHellmanP256);
        //Client's public key is stored in server.
        private static byte[] ClientPubKeyBlob = ClientPrivateKey.Export(CngKeyBlobFormat.EccPublicBlob);
        //Server's public key is stored in client.
        private static byte[] ServerPubKeyBlob = ServerPrivateKey.Export(CngKeyBlobFormat.EccPublicBlob);

        private static Guid SessionKey = Guid.NewGuid(), ChallengeResponse = Guid.NewGuid();

        //This will be generated when the client sends the first packet.
        public static byte[] ClientIV;

        /// <summary>
        /// Helper method to generate an Initialization Vector for the client.
        /// </summary>
        public static void GenerateClientIV()
        {
            AesCryptoServiceProvider AES = new AesCryptoServiceProvider();
            AES.GenerateIV();
            ClientIV = AES.IV;
        }

        /// <summary>
        /// A client requested login.
        /// </summary>
        /// <param name="Client">NetworkClient instance.</param>
        /// <param name="Packet">ProcessedPacket instance.</param>
        public static void InitialClientConnect(NetworkClient Client, ProcessedPacket Packet)
        {
            Console.WriteLine("Server receives encrypted data");

            var aes = new AesCryptoServiceProvider();
            int nBytes = aes.BlockSize >> 3;
            byte[] iv = new byte[nBytes];
            for (int i = 0; i < iv.Length; i++)
                iv[i] = (byte)Packet.ReadByte();

            //Username would normally be used to lookup client's public key in DB (only time such a use is valid).
            string Username = Packet.ReadPascalString();

            PacketStream EncryptedPacket = new PacketStream(0x02, 0);
            EncryptedPacket.WriteHeader();

            MemoryStream StreamToEncrypt = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(StreamToEncrypt);
            Writer.Write((byte)ChallengeResponse.ToByteArray().Length);
            Writer.Write(ChallengeResponse.ToByteArray(), 0, ChallengeResponse.ToByteArray().Length);
            Writer.Write((byte)SessionKey.ToByteArray().Length);
            Writer.Write(SessionKey.ToByteArray(), 0, SessionKey.ToByteArray().Length);

            MemoryStream EncryptedStream = Cryptography.EncryptStream(iv, ServerPrivateKey, ClientPubKeyBlob, 
                StreamToEncrypt);
            EncryptedPacket.WriteUInt16((ushort)((ushort)PacketHeaders.UNENCRYPTED + EncryptedStream.Length));
            EncryptedPacket.Write(EncryptedStream.ToArray(), 0, (int)EncryptedStream.Length);

            Writer.Close();
            Client.Send(EncryptedPacket.ToArray());
        }

        /// <summary>
        /// Initial response from server to client.
        /// </summary>
        /// <param name="Client">A NetworkClient instance.</param>
        /// <param name="Packet">A ProcessedPacket instance.</param>
        public static void HandleServerChallenge(NetworkClient Client, ProcessedPacket Packet)
        {
            byte[] PacketBuf = new byte[Packet.Length];
            Packet.Read(PacketBuf, 0, (int)Packet.Length);
            MemoryStream DecryptedStream = Cryptography.DecryptStream(ClientIV, ClientPrivateKey, ServerPubKeyBlob, 
                new MemoryStream(PacketBuf));
            BinaryReader Reader = new BinaryReader(DecryptedStream);

            byte[] ChallengeResponseBuf = Reader.ReadBytes(Reader.ReadByte());
            byte[] SessionKeyBuf = Reader.ReadBytes(Reader.ReadByte());

            Guid ChallengeResponse = new Guid(ChallengeResponseBuf);
            Guid SessionKey = new Guid(SessionKeyBuf);

            PacketStream ChallengeResponseReply = new PacketStream(0x03, 0);
            ChallengeResponseReply.WriteHeader();

            MemoryStream EncryptedStream = new MemoryStream();
            BinaryWriter Writer = new BinaryWriter(EncryptedStream);
            Writer.Write((byte)ChallengeResponse.ToByteArray().Length);
            Writer.Write(ChallengeResponse.ToByteArray());

            EncryptedStream = Cryptography.EncryptStream(ClientIV, ClientPrivateKey, SessionKey.ToByteArray(), 
                EncryptedStream);

            ChallengeResponseReply.WriteByte((byte)EncryptedStream.ToArray().Length);
            ChallengeResponseReply.WriteBytes(EncryptedStream.ToArray());
        }

        public static void HandleChallengeResponse(NetworkClient Client, ProcessedPacket Packet)
        {

        }
    }
}
