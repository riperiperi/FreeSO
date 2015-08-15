using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Common
{
    public class IPAddress
    {
        public static string Get(HttpListenerRequest httpRequest)
        {
            if (httpRequest.Headers["X-Forwarded-For"] != null)
            {
                return httpRequest.Headers["X-Forwarded-For"];
            }
            return Get(httpRequest.RemoteEndPoint);
        }

        public static string Get(IPEndPoint endpoint)
        {
            return endpoint.Address.ToString();
        }
    }
}
