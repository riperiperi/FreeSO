using Microsoft.AspNetCore.Http;
using System.Linq;

namespace FSO.Server.Api.Core.Utils
{
    public class ApiUtils
    {
        private const string HttpContext = "MS_HttpContext";
        private const string RemoteEndpointMessage =
            "System.ServiceModel.Channels.RemoteEndpointMessageProperty";
        private const string OwinContext = "MS_OwinContext";

        public static string GetIP(HttpRequest request)
        {
            var api = FSO.Server.Api.Core.Api.INSTANCE;
            if (!api.Config.UseProxy)
            {
                return request.HttpContext.Connection.RemoteIpAddress.ToString();
            }
            else
            {
                var ip = "127.0.0.1";
                var xff = request.Headers["X-Forwarded-For"];
                if (xff.Count != 0)
                {
                    ip = xff.First();
                    ip = ip.Substring(ip.IndexOf(",") + 1);
                    var last = ip.LastIndexOf(":");
                    if (last != -1 && last < ip.Length - 5) ip = ip.Substring(0, last);
                }
                return ip;
            }
        }
    }
}