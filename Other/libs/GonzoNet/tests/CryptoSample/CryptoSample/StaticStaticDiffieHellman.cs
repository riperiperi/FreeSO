using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.IO;

namespace CryptoSample
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

        public static ECDiffieHellmanPublicKey ImportKey(string Path)
        {
            ECDiffieHellmanPublicKey Key;
            using (BinaryReader Reader = new BinaryReader(File.Open(Path, FileMode.Open)))
            {
                Key = ECDiffieHellmanCngPublicKey.FromByteArray(Reader.ReadBytes(Reader.ReadByte()), CngKeyBlobFormat.EccPublicBlob);
                return Key;
            }
        }
    }
}
