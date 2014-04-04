using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;

namespace CryptoSample
{
    public class Cryptography
    {
        /// <summary>
        /// Encrypts a stream.
        /// </summary>
        /// <param name="InitializationVector">Initialization vec to be used by AES.</param>
        /// <param name="PrivateKey">Private key to be used.</param>
        /// <param name="PubKeyBlob">Public key blob to be used.</param>
        /// <param name="StreamToEncrypt">The stream to encrypt.</param>
        /// <returns>An encrypted stream.</returns>
        public static MemoryStream EncryptStream(byte[] InitializationVector, CngKey PrivateKey, byte[] PubKeyBlob,
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

                    return new MemoryStream(EncryptedStream.ToArray());
                }
            }
        }

        /// <summary>
        /// Decrypts a ProcessedPacket.
        /// </summary>
        /// <param name="InitializationVector">Initialization vec to be used by AES.</param>
        /// <param name="PrivateKey">Private key to be used.</param>
        /// <param name="PubKeyBlob">Public key blob to be used.</param>
        /// <param name="StreamToDecrypt">The stream to decrypt.</param>
        /// <returns>A decrypted stream.</returns>
        public static MemoryStream DecryptStream(byte[] InitializationVector, CngKey PrivateKey, byte[] PubKeyBlob,
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
