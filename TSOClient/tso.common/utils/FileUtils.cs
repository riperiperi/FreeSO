/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace FSO.Common.Utils
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
