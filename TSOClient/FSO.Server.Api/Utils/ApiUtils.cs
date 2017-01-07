using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace FSO.Server.Api.Utils
{
    public class ApiUtils
    {
        public static string GetIP(HttpRequestMessage Request)
        {
            var ip = "127.0.0.1";
            if (Request.Headers.Contains("X-Forwarded-For"))
            {
                ip = Request.Headers.GetValues("X-Forwarded-For").First();
            }
            return ip;
        }
    }
}