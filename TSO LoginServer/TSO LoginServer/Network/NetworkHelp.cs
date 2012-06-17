/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace TSO_LoginServer.Network
{
    /// <summary>
    /// Static helper functions for network related stuff.
    /// </summary>
    class NetworkHelp
    {
        /// <summary>
        /// Converts an IP address to its string representation.
        /// http://geekswithblogs.net/rgupta/archive/2009/04/29/convert-ip-to-long-and-vice-versa-c.aspx
        /// </summary>
        /// <param name="longIP">The long value to convert.</param>
        /// <returns>The string representation of the IP.</returns>
        static public string LongToIP(long longIP)
        {
            string ip = string.Empty;
            for (int i = 0; i < 4; i++)
            {
                int num = (int)(longIP / Math.Pow(256, (3 - i)));
                longIP = longIP - (long)(num * Math.Pow(256, (3 - i)));
                
                if (i == 0)
                    ip = num.ToString();
                else
                    ip  = ip + "." + num.ToString();
            }
            
            return ip;
        }

        /// <summary>
        /// Converts a string representation of an IP to a long value.
        /// http://geekswithblogs.net/rgupta/archive/2009/04/29/convert-ip-to-long-and-vice-versa-c.aspx
        /// </summary>
        /// <param name="ip">The string representation to convert.</param>
        /// <returns>The IP address as a long value.</returns>
        static public long IP2Long(string ip)
        {
            string[] ipBytes;
            double num = 0;
            
            if(!string.IsNullOrEmpty(ip))
            {
                ipBytes = ip.Split('.');

                for (int i = ipBytes.Length - 1; i >= 0; i--)
                {
                    num += ((int.Parse(ipBytes[i]) % 256) * Math.Pow(256, (3 - i)));
                }
            }
            
            return (long)num;
        }
    }
}
