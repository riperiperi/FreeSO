using System;
using System.Linq;
using System.Security.Cryptography;

namespace FSO.Server.Common
{
    public class PasswordHasher
    {
        public static PasswordHash Hash(string password)
        {
            return Hash(password, "Rfc2898");
        }

        public static PasswordHash Hash(string password, string scheme)
        {
            var schemeImpl = GetScheme(scheme);
            return schemeImpl.Hash(password);
        }

        public static bool Verify(string password, PasswordHash hash)
        {
            return Verify(password, hash, "Rfc2898");
        }

        public static bool Verify(string password, PasswordHash hash, string scheme)
        {
            var schemeImpl = GetScheme(scheme);
            return schemeImpl.Verify(password, hash);
        }

        private static IPasswordHashScheme GetScheme(string scheme)
        {
            switch (scheme)
            {
                case "Rfc2898":
                    return new DefaultPasswordHashScheme();
            }

            throw new Exception("Unknown password hash scheme: " + scheme);
        }
    }

    public class PasswordHash
    {
        public byte[] data;
        public string scheme;
    }

    public class DefaultPasswordHashScheme : IPasswordHashScheme
    {
        public PasswordHash Hash(string password)
        {
            var salt_input = GetStrongRandomBytes(16);
            

            return new PasswordHash()
            {
                scheme = "Rfc2898",
                data = Hash(salt_input, password)
            };
        }

        private byte[] Hash(byte[] salt_input, string password)
        {
            var hasher = new Rfc2898DeriveBytes(System.Text.Encoding.UTF8.GetBytes(password), salt_input, 1000);
            var hash = hasher.GetBytes(64);

            //Encode the salt + hash together
            var result = new byte[1 + 16 + hash.Length];
            result[0] = (byte)16;
            Array.Copy(salt_input, 0, result, 1, salt_input.Length);
            Array.Copy(hash, 0, result, salt_input.Length + 1, hash.Length);

            return result;
        }

        public bool Verify(string password, PasswordHash hash)
        {
            var salt_length = hash.data[0];
            var salt_input = new byte[salt_length];
            Array.Copy(hash.data, 1, salt_input, 0, salt_length);

            var expected = Hash(salt_input, password);
            return expected.SequenceEqual(hash.data);
        }

        private byte[] GetStrongRandomBytes(int numBytes)
        {
            var random_bytes = new byte[numBytes];
            using (RNGCryptoServiceProvider rngCsp = new RNGCryptoServiceProvider())
            {
                rngCsp.GetBytes(random_bytes);
            }
            return random_bytes;
        }
    }

    public interface IPasswordHashScheme
    {
        PasswordHash Hash(string password);
        bool Verify(string password, PasswordHash hash);
    }
}
