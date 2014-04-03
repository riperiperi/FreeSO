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
            Writer.Write(ChallengeResponse.ToByteArray(), 0, ChallengeResponse.ToByteArray().Length);
            Writer.Write(SessionKey.ToByteArray(), 0, SessionKey.ToByteArray().Length);

            MemoryStream EncryptedStream = EncryptStream(iv, ServerPrivateKey, ClientPubKeyBlob, StreamToEncrypt);
            EncryptedPacket.WriteUInt16((ushort)((ushort)PacketHeaders.UNENCRYPTED + EncryptedStream.Length));
            EncryptedPacket.Write(EncryptedStream.ToArray(), 0, (int)EncryptedStream.Length);

            Writer.Close();
            Client.Send(EncryptedPacket.ToArray());
        }

        /// <summary>
        /// Encrypts a stream.
        /// </summary>
        /// <param name="InitializationVector">Initialization vec to be used by AES.</param>
        /// <param name="PrivateKey">Private key to be used.</param>
        /// <param name="PubKeyBlob">Public key blob to be used.</param>
        /// <param name="StreamToEncrypt">The stream to encrypt.</param>
        /// <returns>An encrypted stream.</returns>
        private static MemoryStream EncryptStream(byte[] InitializationVector, CngKey PrivateKey, byte[] PubKeyBlob,
            MemoryStream StreamToEncrypt)
        {
            MemoryStream EncryptedStream = new MemoryStream();

            using (var Algorithm = new ECDiffieHellmanCng(PrivateKey))
            {
                using (CngKey PubKey = CngKey.Import(PubKeyBlob,
                      CngKeyBlobFormat.EccPublicBlob))
                {
                    byte[] SymmetricKey = Algorithm.DeriveKeyMaterial(PubKey);
                    Console.WriteLine("EncryptedStream: Created symmetric key with " +
                        "public key information: {0}", Convert.ToBase64String(SymmetricKey));

                    AesCryptoServiceProvider AES = new AesCryptoServiceProvider();
                    AES.Key = SymmetricKey;
                    AES.IV = InitializationVector;
                    int NBytes = AES.BlockSize >> 3; //No idea...

                    using (ICryptoTransform Encryptor = AES.CreateEncryptor())
                    {
                        byte[] DataToEncrypt = StreamToEncrypt.ToArray();

                        var cs = new CryptoStream(EncryptedStream, Encryptor, CryptoStreamMode.Write);
                        cs.Write(DataToEncrypt, NBytes, DataToEncrypt.Length - NBytes);
                        cs.Close();
                    }

                    AES.Clear();

                    return EncryptedStream;
                }
            }
        }

        private static MemoryStream DecryptStream(byte[] InitializationVector, CngKey PrivateKey, byte[] PubKeyBlob, 
            MemoryStream StreamToDecrypt)
        {
            MemoryStream DecryptedStream = new MemoryStream();

            using (var Algorithm = new ECDiffieHellmanCng(PrivateKey))
            {
                using (CngKey PubKey = CngKey.Import(PubKeyBlob,
                      CngKeyBlobFormat.EccPublicBlob))
                {
                    byte[] SymmetricKey = Algorithm.DeriveKeyMaterial(PubKey);
                    Console.WriteLine("DecryptedStream: Created symmetric key with " +
                        "public key information: {0}", Convert.ToBase64String(SymmetricKey));

                    AesCryptoServiceProvider AES = new AesCryptoServiceProvider();
                    AES.Key = SymmetricKey;
                    AES.IV = InitializationVector;
                    int NBytes = AES.BlockSize >> 3; //No idea...

                    using (ICryptoTransform Decryptor = AES.CreateDecryptor())
                    {
                        byte[] DataToDecrypt = StreamToDecrypt.ToArray();

                        var cs = new CryptoStream(DecryptedStream, Decryptor, CryptoStreamMode.Write);
                        cs.Write(DataToDecrypt, NBytes, DataToDecrypt.Length - NBytes);
                        cs.Close();
                    }

                    AES.Clear();

                    return DecryptedStream;
                }
            }
        }
    }
}
