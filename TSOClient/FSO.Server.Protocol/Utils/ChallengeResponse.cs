using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace FSO.Server.Protocol.Utils
{
    public class ChallengeResponse
    {
        private static int _salt_byte_length = 320;
        private static int _nonce_byte_length = 32;
        private static RNGCryptoServiceProvider _random_crypt_prov = new RNGCryptoServiceProvider();
        private static Random _random_gen = new Random();
        private static int _min_iteration_count = 4000;
        private static int _max_iteration_count = 5000;
        private const int _sha_output_length = 20;

        private static byte[] ComputeHMACHash(byte[] data, string secret)
        {
            using (var _hmac_sha_1 = new HMACSHA1(data, true))
            {
                byte[] _hash_bytes = _hmac_sha_1.ComputeHash(Encoding.UTF8.GetBytes(secret));
                return _hash_bytes;
            }
        }

        public static string AnswerChallenge(string challenge, string secret)
        {
            var components = new Dictionary<string, string>();
            var parts = challenge.Split(',');
            foreach(var part in parts){
                var subParts = part.Split(new char[] { '=' }, 2);
                components.Add(subParts[0], subParts[1]);
            }

            var iterations = int.Parse(components["i"]);
            var salt = components["s"];
            var salted = Hi(secret, Convert.FromBase64String(salt), iterations);

            return Convert.ToBase64String(ComputeHMACHash(salted, secret));
        }

        public static string GetChallenge()
        {
            var numIterations = GetRandomInteger();
            return "i=" + numIterations + ",s=" + Convert.ToBase64String(GetRandomByteArray(_nonce_byte_length));
        }

        private static byte[] Hi(string password, byte[] salt, int iteration_count)
        {
            Rfc2898DeriveBytes _pdb = new Rfc2898DeriveBytes(password, salt, iteration_count);
            return _pdb.GetBytes(_sha_output_length);
        }

        private static int GetRandomInteger()
        {
            if (_random_gen == null)
                _random_gen = new Random();

            int _random_int = _random_gen.Next(_min_iteration_count, _max_iteration_count);

            if (_random_int < 0)
                _random_int *= -1;

            return _random_int;
        }

        private static byte[] GetRandomByteArray(int byte_array_length)
        {
            byte[] _random_byte_array = new byte[byte_array_length];

            _random_crypt_prov.GetBytes(_random_byte_array);

            return _random_byte_array;
        }
    }
}
