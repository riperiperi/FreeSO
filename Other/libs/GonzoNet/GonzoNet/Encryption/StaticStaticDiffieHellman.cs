/*

using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using GonzoNet.Exceptions;

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

        /// <summary>
        /// Encrypts the provided data.
        /// </summary>
        /// <param name="privateKey">The private key used for encryption.</param>
        /// <param name="publicKey">The public key used for encryption.</param>
        /// <param name="nonce">The nonce used for encryption.</param>
        /// <param name="data">The data to encrypt.</param>
        /// <returns>The encrypted data.</returns>
        public static byte[] Encrypt(ECDiffieHellmanCng privateKey, ECDiffieHellmanPublicKey publicKey, byte[] nonce, byte[] data)
        {
            Aes aes = DeriveKeyAndIv(privateKey, publicKey, nonce);
            return aes.CreateEncryptor().TransformFinalBlock(data, 0, data.Length);
        }

        /// <summary>
        /// Decrypts the provided data.
        /// </summary>
        /// <param name="privateKey">The private key used for decryption.</param>
        /// <param name="publicKey">The public key used for decryption.</param>
        /// <param name="nonce">The nonce used for decryption.</param>
        /// <param name="encryptedData">The data to decrypt.</param>
        /// <returns>The decrypted data.</returns>
        public static byte[] Decrypt(ECDiffieHellmanCng privateKey, ECDiffieHellmanPublicKey publicKey, byte[] nonce, byte[] encryptedData)
        {
            try
            {
                Aes aes = DeriveKeyAndIv(privateKey, publicKey, nonce);
                return aes.CreateDecryptor().TransformFinalBlock(encryptedData, 0, encryptedData.Length);
            }
            catch (Exception E)
            {
                throw new DecryptionException(E.ToString());
            }
        }

        public static void ExportKey(string Path, byte[] Key)
        {
            using (BinaryWriter Writer = new BinaryWriter(File.Create(Path)))
            {
                Writer.Write((byte)Key.Length);
                Writer.Write(Key);
            }
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
        /// Imports a key from a specified path.
        /// </summary>
        /// <param name="Path">The path of the key.</param>
        /// <returns>A key in the form of an array of bytes, or null if something went haywire.</returns>
        public static byte[] ImportKey(string Path)
        {
            try
            {
                using (BinaryReader Reader = new BinaryReader(File.Open(Path, FileMode.Open, FileAccess.Read, FileShare.Read)))
                {
                    return Reader.ReadBytes(Reader.ReadByte());
                }
            }
            catch (Exception)
            {
                Logger.Log("StaticStaticDiffieHellman: Couldn't load key!", LogLevel.warn);
                return null;
            }
        }
    }
}
*/