using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace GonzoNet.Encryption
{
    /// <summary>
    /// Contains methods for en/decryption and ex/importing keys.
    /// From: http://stackoverflow.com/questions/3196297/minimal-message-size-public-key-encryption-in-net
    /// </summary>
    public static class StaticStaticDiffieHellman
    {
        private static Aes DeriveKeyAndIv(ECDiffieHellmanCng privateKey, ECDiffieHellmanPublicKey publicKey, byte[] nonce)
        {
            privateKey.KeyDerivationFunction = ECDiffieHellmanKeyDerivationFunction.Hash;
            privateKey.HashAlgorithm = CngAlgorithm.Sha256;
            privateKey.SecretAppend = nonce;
            byte[] keyAndIv = privateKey.DeriveKeyMaterial(publicKey);
            byte[] key = new byte[16];
            Array.Copy(keyAndIv, 0, key, 0, 16);
            byte[] iv = new byte[16];
            Array.Copy(keyAndIv, 16, iv, 0, 16);

            Aes aes = new AesManaged();
            aes.Key = key;
            aes.IV = iv;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;

            return aes;
        }

        public static byte[] Encrypt(ECDiffieHellmanCng privateKey, ECDiffieHellmanPublicKey publicKey, byte[] nonce, byte[] data)
        {
            Aes aes = DeriveKeyAndIv(privateKey, publicKey, nonce);
            return aes.CreateEncryptor().TransformFinalBlock(data, 0, data.Length);
        }

        public static byte[] Decrypt(ECDiffieHellmanCng privateKey, ECDiffieHellmanPublicKey publicKey, byte[] nonce, byte[] encryptedData)
        {
            Aes aes = DeriveKeyAndIv(privateKey, publicKey, nonce);
            return aes.CreateDecryptor().TransformFinalBlock(encryptedData, 0, encryptedData.Length);
        }

        public static void ExportKey(string Path, ECDiffieHellmanPublicKey Key)
        {
            using (BinaryWriter Writer = new BinaryWriter(File.Create(Path)))
            {
                Writer.Write((byte)Key.ToByteArray().Length);
                Writer.Write(Key.ToByteArray());
            }
        }

        /// <summary>
        /// Exports a private key.
        /// </summary>
        /// <param name="Path">The path to export to.</param>
        /// <param name="PrivateKey">The key to export.</param>
        public static void ExportKey(string Path, CngKey PrivateKey)
        {
            using (BinaryWriter Writer = new BinaryWriter(File.Create(Path)))
            {
                Writer.Write((byte)PrivateKey.Export(CngKeyBlobFormat.EccPrivateBlob).Length);
                Writer.Write(PrivateKey.Export(CngKeyBlobFormat.EccPrivateBlob));
            }
        }

        /// <summary>
        /// Imports a key.
        /// </summary>
        /// <param name="Path">The path to the file containing the key.</param>
        /// <param name="PrivateKey">Is the key private?</param>
        /// <returns>A ECDiffieHellmanPublicKey instance.</returns>
        public static ECDiffieHellmanPublicKey ImportKey(string Path, bool PrivateKey)
        {
            ECDiffieHellmanPublicKey Key;
            using (BinaryReader Reader = new BinaryReader(File.Open(Path, FileMode.Open)))
            {
                if(PrivateKey)
                    Key = ECDiffieHellmanCngPublicKey.FromByteArray(Reader.ReadBytes(Reader.ReadByte()), CngKeyBlobFormat.EccPrivateBlob);
                else
                    Key = ECDiffieHellmanCngPublicKey.FromByteArray(Reader.ReadBytes(Reader.ReadByte()), CngKeyBlobFormat.EccPublicBlob);
                return Key;
            }
        }
    }
}
