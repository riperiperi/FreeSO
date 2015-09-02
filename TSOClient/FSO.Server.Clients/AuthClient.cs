using FSO.Server.Clients.Framework;
using FSO.Server.Protocol.Authorization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;

namespace FSO.Server.Clients
{
    public class AuthClient : AbstractHttpClient
    {
        public AuthClient(string baseUrl) : base(baseUrl) {
        }

        public async Task<AuthResult> Authenticate(AuthRequest request)
        {
            var url = "/AuthLogin?username=" + HttpUtility.UrlEncode(request.Username) +
                        "&password=" + HttpUtility.UrlEncode(request.Password) +
                        "&serviceID=" + HttpUtility.UrlEncode(request.ServiceID) +
                        "&version=" + HttpUtility.UrlEncode(request.Version);

            var client = CreateClient();
            var httpRequest = client.GetAsync(url);
            var response = httpRequest.Result;
            response.EnsureSuccessStatusCode();

            var responseText = await response.Content.ReadAsStringAsync();
            
            var result = new AuthResult();
            var lines = responseText.Split(new char[] { '\n' });
            foreach (var line in lines)
            {
                var components = line.Trim().Split(new char[] { '=' }, 2);
                if (components.Length != 2) { continue; }

                switch (components[0])
                {
                    case "Valid":
                        result.Valid = Boolean.Parse(components[1]);
                        break;
                    case "Ticket":
                        result.Ticket = components[1];
                        break;
                    case "reasoncode":
                        result.ReasonCode = components[1];
                        break;
                    case "reasontext":
                        result.ReasonText = components[1];
                        break;
                    case "reasonurl":
                        result.ReasonURL = components[1];
                        break;
                }
            }
            return result;
        }
    }
}
