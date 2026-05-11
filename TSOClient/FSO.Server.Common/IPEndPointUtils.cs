using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace FSO.Server.Common
{
    public class IPEndPointUtils
    {
        // Note: this namespace defines its own IPAddress class, which
        // shadows System.Net.IPAddress for unqualified references inside
        // FSO.Server.Common. Every reference to the BCL type below is
        // fully qualified for that reason.

        // Single resolved endpoint. Convenience wrapper around ResolveAll
        // that returns the first (preferred) entry — IPv4 if the host has
        // both A and AAAA records.
        public static IPEndPoint CreateIPEndPoint(string endPoint)
        {
            var all = ResolveAll(endPoint);
            if (all.Length == 0)
                throw new FormatException("Could not resolve " + endPoint);
            return all[0];
        }

        // All endpoints the input resolves to, sorted IPv4-first then IPv6.
        // Callers (AriesClient, FSOSandboxClient) use this for happy-eyeballs:
        // try v4, fall back to v6 if v4's path is unreachable. Hosts publishing
        // only AAAA still resolve via the second slot.
        //
        // Accepts:
        //   "1.2.3.4:33101"        — single v4 literal
        //   "[::1]:33101"          — single v6 literal (brackets required for v6 literals so port-colon parsing is unambiguous)
        //   "host.example:33101"   — DNS lookup, returns every address the host publishes
        public static IPEndPoint[] ResolveAll(string endPoint)
        {
            if (string.IsNullOrEmpty(endPoint))
                throw new FormatException("Invalid endpoint format");

            // Split on the last colon so IPv6 literals (multiple colons) parse
            // the same as v4 literals.
            int colon = endPoint.LastIndexOf(':');
            if (colon <= 0 || colon == endPoint.Length - 1)
                throw new FormatException("Invalid endpoint format");

            string host = endPoint.Substring(0, colon);
            string portStr = endPoint.Substring(colon + 1);

            if (host.Length >= 2 && host[0] == '[' && host[host.Length - 1] == ']')
                host = host.Substring(1, host.Length - 2);

            if (!int.TryParse(portStr, NumberStyles.None, NumberFormatInfo.CurrentInfo, out int port))
                throw new FormatException("Invalid port");

            System.Net.IPAddress[] addrs;
            if (System.Net.IPAddress.TryParse(host, out var literal))
            {
                addrs = new[] { literal };
            }
            else
            {
                addrs = Dns.GetHostEntry(host).AddressList;
                if (addrs.Length == 0)
                    throw new FormatException("Invalid ip-address");
            }

            // v4 first (preferred), v6 second (fallback). Stable within each
            // family so DNS-returned ordering is preserved among same-family
            // siblings.
            var ordered = addrs
                .Select((a, i) => new { a, i })
                .OrderBy(x => x.a.AddressFamily == AddressFamily.InterNetwork ? 0 : 1)
                .ThenBy(x => x.i)
                .Select(x => new IPEndPoint(x.a, port))
                .ToArray();

            return ordered;
        }

        // For server bindings: if the host is the v4 wildcard "0.0.0.0" or the
        // v6 wildcard "::", expand to BOTH families so the listener accepts
        // connections on either stack. Specific IPs (e.g. 192.168.1.1:33100)
        // are returned verbatim — operators who pin a specific address know
        // which family they want.
        //
        // .NET Core 2.2 / Mina.NET creates IPv6 sockets with IPV6_V6ONLY=1 by
        // default, so a single "[::]" bind doesn't accept v4 traffic — hence
        // the explicit dual bind. Each returned endpoint becomes its own
        // IoAcceptor instance.
        public static IPEndPoint[] ExpandWildcardBindings(string binding)
        {
            if (string.IsNullOrEmpty(binding))
                throw new FormatException("Invalid binding format");

            int colon = binding.LastIndexOf(':');
            if (colon <= 0 || colon == binding.Length - 1)
                throw new FormatException("Invalid binding format");

            string host = binding.Substring(0, colon);
            string portStr = binding.Substring(colon + 1);

            if (host.Length >= 2 && host[0] == '[' && host[host.Length - 1] == ']')
                host = host.Substring(1, host.Length - 2);

            if (!int.TryParse(portStr, NumberStyles.None, NumberFormatInfo.CurrentInfo, out int port))
                throw new FormatException("Invalid port");

            if (host == "0.0.0.0" || host == "::")
            {
                return new[]
                {
                    new IPEndPoint(System.Net.IPAddress.Any, port),
                    new IPEndPoint(System.Net.IPAddress.IPv6Any, port),
                };
            }

            // Specific IP — caller asked for exactly this. Don't second-guess.
            return new[] { CreateIPEndPoint(binding) };
        }
    }
}
