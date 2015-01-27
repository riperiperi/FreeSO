using System;
using System.Collections.Generic;
using System.Text;

namespace TSOClient
{
    public static class FNV
    {
        public static uint HashString32(string str)
        {
            if (str == null)
            {
                return 0;
            }
            uint num2 = 0x811c9dc5;
            byte[] bytes = new ASCIIEncoding().GetBytes(str);
            int length = bytes.Length;
            int index = 0;
            while (length > 0)
            {
                num2 = (num2 * 0x1000193) ^ bytes[index];
                index++;
                length--;
            }
            return num2;
        }
    }
}
