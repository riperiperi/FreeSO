using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace TSOClient.Code.Utils
{
    public class FileUtils
    {
        public static string ComputeMD5(string filePath){
            var bytes = ComputeMD5Bytes(filePath);
            return BitConverter.ToString(bytes).Replace("-", "").ToLower();
        }

        public static byte[] ComputeMD5Bytes(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    return md5.ComputeHash(stream);
                }
            }
        }
    }
}
