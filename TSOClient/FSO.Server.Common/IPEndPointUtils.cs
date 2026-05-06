using System;
using System.Globalization;
using System.Net;
using System.Net.Sockets;

// FSO.Server.Common defines its own IPAddress class; alias the BCL type
// to avoid the name collision inside this file.
using IPAddress = System.Net.IPAddress;

namespace FSO.Server.Common
{
    public class IPEndPointUtils
    {
        public static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            if (string.IsNullOrEmpty(endPoint))
                throw new FormatException("Invalid endpoint format");

            // Split on the last colon so IPv6 literals (which contain
            // multiple colons) parse the same as v4 literals.
            int colon = endPoint.LastIndexOf(':');
            if (colon <= 0 || colon == endPoint.Length - 1)
                throw new FormatException("Invalid endpoint format");

            string host = endPoint.Substring(0, colon);
            string portStr = endPoint.Substring(colon + 1);

            if (host.Length >= 2 && host[0] == '[' && host[host.Length - 1] == ']')
                host = host.Substring(1, host.Length - 2);

            IPAddress ip;
            if (!IPAddress.TryParse(host, out ip))
            {
                var addrs = Dns.GetHostEntry(host).AddressList;
                if (addrs.Length == 0)
                    throw new FormatException("Invalid ip-address");

                // Prefer IPv6 when the host publishes both — a v6-only
                // client can't reach a v4 address, but a dual-stack client
                // can reach either.
                ip = Array.Find(addrs, a => a.AddressFamily == AddressFamily.InterNetworkV6) ?? addrs[0];
            }

            int port;
            if (!int.TryParse(portStr, NumberStyles.None, NumberFormatInfo.CurrentInfo, out port))
                throw new FormatException("Invalid port");

            return new IPEndPoint(ip, port);
        }
    }
}