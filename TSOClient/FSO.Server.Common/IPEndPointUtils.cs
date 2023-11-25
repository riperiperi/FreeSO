using System;
using System.Globalization;
using System.Net;

namespace FSO.Server.Common
{
    public class IPEndPointUtils
    {
        public static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            string[] ep = endPoint.Split(':');
            if (ep.Length != 2) throw new FormatException("Invalid endpoint format");
            System.Net.IPAddress ip;
            if (!System.Net.IPAddress.TryParse(ep[0], out ip))
            {
                var addrs = Dns.GetHostEntry(ep[0]).AddressList;
                if (addrs.Length == 0)
                {
                    throw new FormatException("Invalid ip-address");
                }
                else ip = addrs[0];
            }

            int port;
            if (!int.TryParse(ep[1], NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
            {
                throw new FormatException("Invalid port");
            }
            return new IPEndPoint(ip, port);
        }
    }
}
