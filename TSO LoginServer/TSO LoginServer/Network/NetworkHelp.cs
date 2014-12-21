/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
