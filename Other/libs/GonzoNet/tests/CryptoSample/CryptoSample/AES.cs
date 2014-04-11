using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CryptoSample
{
    using System;
    using System.Text;
    using System.Security.Cryptography;

    /// <summary>
    /// AES class is derived from the MSDN .NET CreateEncryptor() example
    /// http://msdn.microsoft.com/en-us/library/09d0kyb3.aspx
    /// </summary>
    class AES
    {
        // Symmetric algorithm interface is used to store the AES service provider
        private SymmetricAlgorithm AESProvider;

        // Crytographic transformers are used to encrypt and decrypt byte arrays
        private ICryptoTransform encryptor;
        private ICryptoTransform decryptor;

        /// <summary>
        /// Constructor for AES class that takes byte arrays for the key and IV
        /// </summary>
        /// <param name="key">Cryptographic key</param>
        /// <param name="IV">Cryptographic initialization vector</param>
        public AES(byte[] key, byte[] IV)
        {
            // Initialize AESProvider with AES service provider
            AESProvider = new AesCryptoServiceProvider();

            // Set the key and IV for AESProvider
            AESProvider.Key = key;
            AESProvider.IV = IV;

            // Initialize cryptographic transformers from AESProvider
            encryptor = AESProvider.CreateEncryptor();
            decryptor = AESProvider.CreateDecryptor();
        }

        /// <summary>
        /// Constructor for AES class that generates the key and IV from salted passwords
        /// </summary>
        /// <param name="keyPassword">Password used to generate the key</param>
        /// <param name="IVPassword">Password used to generate the IV</param>
        /// <param name="salt">Salt used to secure the passwords</param>
        public AES(string keyPassword, string IVPassword, string salt)
        {
            // Initialize AESProvider with AES service provider
            AESProvider = new AesCryptoServiceProvider();

            // Initialize a hasher with the default MD5 algorithm
            MD5 md5 = System.Security.Cryptography.MD5.Create();

            // Generate the key and IV for AESProvider from hashed, salted passwords
            AESProvider.Key = md5.ComputeHash(UnicodeEncoding.Unicode.GetBytes(keyPassword + salt));
            AESProvider.IV = md5.ComputeHash(UnicodeEncoding.Unicode.GetBytes(IVPassword + salt));

            // Initialize cryptographic transformers from AESProvider
            encryptor = AESProvider.CreateEncryptor();
            decryptor = AESProvider.CreateDecryptor();
        }

        /// <summary>
        /// Encrypts a string with AES
        /// </summary>
        /// <param name="plainText">String to encrypt</param>
        /// <returns>Encrypted string</returns>
        public string Encrypt(string plainText)
        {
            // Convert string to bytes
            byte[] plainBytes = UnicodeEncoding.Unicode.GetBytes(plainText);

            // Encrypt bytes
            byte[] secureBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

            // Return encrypted bytes as a string
            return UnicodeEncoding.Unicode.GetString(secureBytes);
        }

        /// <summary>
        /// Encrypts a byte array with AES
        /// </summary>
        /// <param name="plainBytes">Data to encrypt</param>
        /// <returns>Encrypted byte array</returns>
        public byte[] Encrypt(byte[] plainBytes)
        {
            // Encrypt bytes
            return encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);
        }

        /// <summary>
        /// Decrypts a string with AES
        /// </summary>
        /// <param name="secureText">Encrypted string to decrypt</param>
        /// <returns>Decrypted string</returns>
        public string Decrypt(string secureText)
        {
            // Convert encrypted string to bytes
            byte[] secureBytes = UnicodeEncoding.Unicode.GetBytes(secureText);

            // Decrypt bytes
            byte[] plainBytes = decryptor.TransformFinalBlock(secureBytes, 0, secureBytes.Length);

            // Return decrypted bytes as a string
            return UnicodeEncoding.Unicode.GetString(plainBytes);
        }

        /// <summary>
        /// Decrypts data with AES
        /// </summary>
        /// <param name="secureBytes">Encrypted data to decrypt</param>
        /// <returns>Decrypted data</returns>
        public byte[] Decrypt(byte[] secureBytes)
        {
            // Decrypt bytes
            return decryptor.TransformFinalBlock(secureBytes, 0, secureBytes.Length);
        }
    }
}
