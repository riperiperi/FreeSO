using FSO.Common.Utils;
using FSO.Server.Clients.Framework;
using Newtonsoft.Json;
using RestSharp;

namespace FSO.Server.Clients
{
    public class ApiClient : AbstractHttpClient, IDisposable
    {
        private RestClient client;
        public static string CDNUrl;

        public static string AuthKey;

        public ApiClient(string baseUrl) : base(baseUrl)
        {
            client = Client();
        }

        public async Task GetThumbnailAsync(uint shardID, uint location, Action<byte[]> callback)
        {
            var client = Client();
            var request = new RestRequest($"userapi/city/{shardID}/{location}.png", Method.Get);

            try
            {
                var response = await client.ExecuteAsync(request); // no lambda, returns RestResponse

                GameThread.NextUpdate(_ =>
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        callback(null);
                    else
                        callback(response.RawBytes);
                });
            }
            catch
            {
                GameThread.NextUpdate(_ => callback(null));
            }
        }

        public async Task GetFacadeAsync(uint shardID, uint location, Action<byte[]> callback)
        {
            var client = Client();
            var request = new RestRequest($"userapi/city/{shardID}/{location}.fsof", Method.Get);

            try
            {
                var response = await client.ExecuteAsync(request);

                GameThread.NextUpdate(_ =>
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK)
                        callback(null);
                    else
                        callback(response.RawBytes);
                });
            }
            catch
            {
                GameThread.NextUpdate(_ => callback(null));
            }
        }


        public async Task AdminLoginAsync(string username, string password, Action<bool> callback)
        {
            var client = Client();
            var request = new RestRequest("admin/oauth/token", Method.Post);

            request.AddParameter("application/x-www-form-urlencoded",
                $"grant_type=password&username={username}&password={password}",
                ParameterType.RequestBody);

            try
            {
                var response = await client.ExecuteAsync(request);

                bool ok = response.StatusCode == System.Net.HttpStatusCode.OK;

                if (ok)
                {
                    // Deserialize the token
                    dynamic obj = JsonConvert.DeserializeObject(response.Content);
                    AuthKey = obj.access_token;
                }

                // Call back on the game thread
                GameThread.NextUpdate(_ => callback(ok));
            }
            catch
            {
                GameThread.NextUpdate(_ => callback(false));
            }
        }

        public async Task GetWork(Action<int, uint> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/city/thumbwork.json", Method.Get);
            request.AddHeader("authorization", "bearer " + AuthKey);

            try
            {
                var response = await client.ExecuteAsync(request);
                bool ok = response.StatusCode == System.Net.HttpStatusCode.OK;

                GameThread.NextUpdate(_ =>
                {
                    if (!ok || string.IsNullOrEmpty(response.Content))
                        callback(-1, ok ? 0 : uint.MaxValue);
                    else
                    {
                        dynamic obj = JsonConvert.DeserializeObject(response.Content);
                        callback(Convert.ToInt32(obj.shard_id), Convert.ToUInt32(obj.location));
                    }
                });
            }
            catch
            {
                GameThread.NextUpdate(_ => callback(-1, uint.MaxValue));
            }
        }

        public async Task GetFSOV(uint shardID, uint lotLocation, Action<byte[]> callback)
        {
            var client = Client();
            var request = new RestRequest($"userapi/city/{shardID}/{lotLocation}.fsov", Method.Get);
            request.AddHeader("authorization", "bearer " + AuthKey);

            try
            {
                var response = await client.ExecuteAsync(request);
                bool ok = response.StatusCode == System.Net.HttpStatusCode.OK;
                byte[] dat = response.RawBytes;

                GameThread.NextUpdate(_ =>
                {
                    callback(ok ? dat : null);
                });
            }
            catch
            {
                GameThread.NextUpdate(_ => callback(null));
            }
        }

        public async Task UploadFSOF(uint shardID, uint lotLocation, byte[] data, Action<bool> callback)
        {
            var client = Client();
            var request = new RestRequest($"userapi/city/{shardID}/uploadfacade/{lotLocation}", Method.Post);

            request.AddFile("files", data, $"{lotLocation}.fsof", "application/octet-stream");
            request.AddHeader("authorization", "bearer " + AuthKey);

            try
            {
                var response = await client.ExecuteAsync(request); // returns RestResponse
                bool ok = response.StatusCode == System.Net.HttpStatusCode.OK;
                Console.WriteLine(response.StatusCode);

                GameThread.NextUpdate(_ => callback(ok));
            }
            catch
            {
                GameThread.NextUpdate(_ => callback(false));
            }
        }

        public async Task UploadThumb(uint shardID, uint lotLocation, byte[] data, Action<bool> callback)
        {
            var client = Client();
            var request = new RestRequest($"userapi/city/{shardID}/uploadthumb/{lotLocation}", Method.Post);

            request.AddFile("files", data, $"{lotLocation}.png", "application/octet-stream");
            request.AddHeader("authorization", "bearer " + AuthKey);

            try
            {
                var response = await client.ExecuteAsync(request); // returns RestResponse
                bool ok = response.StatusCode == System.Net.HttpStatusCode.OK;
                Console.WriteLine(response.StatusCode);

                GameThread.NextUpdate(_ => callback(ok));
            }
            catch
            {
                GameThread.NextUpdate(_ => callback(false));
            }
        }

        public async Task GetLotList(uint shardID, Action<uint[]> callback)
        {
            var client = Client();
            var request = new RestRequest($"userapi/city/{shardID}/city.json", Method.Get);

            try
            {
                var response = await client.ExecuteAsync(request); // returns RestResponse

                GameThread.NextUpdate(_ =>
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK || string.IsNullOrEmpty(response.Content))
                    {
                        callback(null);
                    }
                    else
                    {
                        dynamic obj = JsonConvert.DeserializeObject(response.Content);
                        Newtonsoft.Json.Linq.JArray data = obj.reservedLots;
                        uint[] result = data.Select(y => Convert.ToUInt32(y)).ToArray();
                        callback(result);
                    }
                });
            }
            catch
            {
                GameThread.NextUpdate(_ => callback(null));
            }
        }

        public async Task GetUpdateList(Action<ApiUpdate[]> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/update", Method.Get);

            try
            {
                var response = await client.ExecuteAsync(request); // returns RestResponse

                GameThread.NextUpdate(_ =>
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK || string.IsNullOrEmpty(response.Content))
                    {
                        callback(null);
                    }
                    else
                    {
                        var obj = JsonConvert.DeserializeObject<ApiUpdate[]>(response.Content);
                        callback(obj);
                    }
                });
            }
            catch
            {
                GameThread.NextUpdate(_ => callback(null));
            }
        }

        public async Task GetUpdateList(string branchName, Action<ApiUpdate[]> callback)
        {
            var client = Client();
            var request = new RestRequest($"userapi/updates/{branchName}", Method.Get);

            try
            {
                var response = await client.ExecuteAsync(request); // returns RestResponse

                GameThread.NextUpdate(_ =>
                {
                    if (response.StatusCode != System.Net.HttpStatusCode.OK || string.IsNullOrEmpty(response.Content))
                    {
                        callback(null);
                    }
                    else
                    {
                        var obj = JsonConvert.DeserializeObject<ApiUpdate[]>(response.Content);
                        callback(obj);
                    }
                });
            }
            catch
            {
                GameThread.NextUpdate(_ => callback(null));
            }
        }

        public async Task<ApiStatus> GetStatus()
        {
            var client = Client();
            var request = new RestRequest($"userapi/status.json", Method.Get)
            {
                Timeout = TimeSpan.FromSeconds(2)
            };

            try
            {
                RestResponse response = await client.ExecuteAsync(request);

                if (response.StatusCode != System.Net.HttpStatusCode.OK || string.IsNullOrEmpty(response.Content))
                {
                    return null;
                }
                else
                {
                    var obj = JsonConvert.DeserializeObject<ApiStatus>(response.Content);
                    return obj;
                }
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            client.Dispose();
        }
    }
}
