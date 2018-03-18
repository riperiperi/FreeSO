using FSO.Common.Utils;
using FSO.Server.Clients.Framework;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Clients
{
    public class ApiClient : AbstractHttpClient
    {
        private RestClient client;
        public static string CDNUrl;

        public ApiClient(string baseUrl) : base(baseUrl) {
            client = Client();
        }

        public void GetThumbnailAsync(uint shardID, uint location, Action<byte[]> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/city/" + shardID + "/" + location + ".png");

            client.ExecuteAsync(request, (resp, h) =>
            {
                GameThread.NextUpdate(x =>
                {
                    if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                        callback(null);
                    else
                        callback(resp.RawBytes);
                });
            });
        }
    }
}
