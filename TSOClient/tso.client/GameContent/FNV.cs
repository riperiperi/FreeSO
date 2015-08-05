/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text;

namespace FSO.Client.GameContent
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
