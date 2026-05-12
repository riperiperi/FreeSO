using FSO.Common.Utils;
using FSO.Server.Clients.Framework;
using Newtonsoft.Json;
using RestSharp;
using System;
using System.Linq;

namespace FSO.Server.Clients
{
    public class ApiClient : AbstractHttpClient
    {
        private RestClient client;
        public static string CDNUrl;

        public static string AuthKey;

        public ApiClient(string baseUrl) : base(baseUrl) {
            client = Client();
        }

        public void GetThumbnailAsync(uint shardID, uint location, Action<byte[]> callback)
        {
            //var client = Client();
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

        public void GetFacadeAsync(uint shardID, uint location, Action<byte[]> callback)
        {
            //var client = Client();
            var request = new RestRequest("userapi/city/" + shardID + "/" + location + ".fsof");

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


        public void AdminLogin(string username, string password, Action<bool> callback)
        {
            var client = Client();
            var request = new RestRequest("admin/oauth/token", Method.POST);
            request.AddParameter("application/x-www-form-urlencoded",
                "grant_type=password&username="+username+"&password="+password,
                ParameterType.RequestBody);

            client.ExecuteAsync(request, (resp, h) =>
            {
                var ok = resp.StatusCode == System.Net.HttpStatusCode.OK;
                if (ok) { 
                    dynamic obj = JsonConvert.DeserializeObject(resp.Content);
                    AuthKey = obj.access_token;
                }
                GameThread.NextUpdate(x =>
                {
                    callback(ok);
                });
            });
        }

        public void GetWork(Action<int, uint> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/city/thumbwork.json", Method.GET);
            request.AddHeader("authorization", "bearer " + AuthKey);

            client.ExecuteAsync(request, (resp, h) =>
            {
                var ok = resp.StatusCode == System.Net.HttpStatusCode.OK;
                
                GameThread.NextUpdate(x =>
                {
                    if (!ok || resp.Content == "") callback(-1, (ok)?0:uint.MaxValue);
                    else
                    {
                        dynamic obj = JsonConvert.DeserializeObject(resp.Content);
                        callback(Convert.ToInt32(obj.shard_id), Convert.ToUInt32(obj.location));
                    }
                });
            });
        }

        public void GetFSOV(uint shardID, uint lotLocation, Action<byte[]> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/city/"+shardID+"/"+lotLocation+".fsov", Method.GET);
            request.AddHeader("authorization", "bearer " + AuthKey);

            client.ExecuteAsync(request, (resp, h) =>
            {
                var ok = resp.StatusCode == System.Net.HttpStatusCode.OK;
                byte[] dat = resp.RawBytes;
                GameThread.NextUpdate(x =>
                {
                    callback(ok?dat:null);
                });
            });
        }

        public void UploadFSOF(uint shardID, uint lotLocation, byte[] data, Action<bool> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/city/" + shardID + "/uploadfacade/" + lotLocation, Method.POST);
            request.AddFile("files", data, lotLocation + ".fsof", "application/octet-stream");
            request.AddHeader("authorization", "bearer " + AuthKey);

            client.ExecuteAsync(request, (resp, h) =>
            {
                var ok = resp.StatusCode == System.Net.HttpStatusCode.OK;
                Console.WriteLine(resp.StatusCode);
                GameThread.NextUpdate(x =>
                {
                    callback(ok);
                });
            });
        }

        public void GetLotList(uint shardID, Action<uint[]> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/city/" + shardID + "/city.json");

            client.ExecuteAsync(request, (resp, h) =>
            {
                GameThread.NextUpdate(x =>
                {
                    if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                        callback(null);
                    else
                    {
                        dynamic obj = JsonConvert.DeserializeObject(resp.Content);
                        Newtonsoft.Json.Linq.JArray data = obj.reservedLots;
                        uint[] result = data.Select(y => Convert.ToUInt32(y)).ToArray();
                        callback(result);
                    }
                });
            });
        }

        // Daily quests — see edenso_server_data/design_daily_quests_v1.md.
        // Both endpoints require the JWT minted by InitialConnectServlet.
        // Caller passes the token explicitly (typically GameFacade.ApiAuthToken
        // which LoginRegulator sets at login). Null/empty token still issues
        // the request but the server will return 401 — callers can use that
        // as the "not logged in" signal.
        public void GetDailyQuests(uint avatarId, string authToken, Action<ApiDailyQuestList> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/quests/today");
            request.AddParameter("avatar_id", avatarId);
            if (!string.IsNullOrEmpty(authToken))
                request.AddHeader("Authorization", "Bearer " + authToken);

            client.ExecuteAsync(request, (resp, h) =>
            {
                GameThread.NextUpdate(x =>
                {
                    if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                        callback(null);
                    else
                        callback(JsonConvert.DeserializeObject<ApiDailyQuestList>(resp.Content));
                });
            });
        }

        // Returns ApiDailyQuestClaimResult on success or null on any non-200
        // (already claimed, not yet completed, 401 expired, etc.). Caller
        // should refresh the dialog after either outcome so the UI reflects
        // reality.
        public void ClaimDailyQuest(uint avatarId, byte slot, string authToken, Action<ApiDailyQuestClaimResult> callback)
        {
            var client = Client();
            var request = new RestRequest($"userapi/quests/claim/{slot}", Method.POST);
            request.AddParameter("avatar_id", avatarId);
            if (!string.IsNullOrEmpty(authToken))
                request.AddHeader("Authorization", "Bearer " + authToken);

            client.ExecuteAsync(request, (resp, h) =>
            {
                GameThread.NextUpdate(x =>
                {
                    if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                        callback(null);
                    else
                        callback(JsonConvert.DeserializeObject<ApiDailyQuestClaimResult>(resp.Content));
                });
            });
        }

        public void GetUpdateList(Action<ApiUpdate[]> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/update");

            client.ExecuteAsync(request, (resp, h) =>
            {
                GameThread.NextUpdate(x =>
                {
                    if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                        callback(null);
                    else
                    {
                        var obj = JsonConvert.DeserializeObject<ApiUpdate[]>(resp.Content);
                        callback(obj);
                    }
                });
            });
        }

        public void GetUpdateList(string branchName, Action<ApiUpdate[]> callback)
        {
            var client = Client();
            var request = new RestRequest("userapi/updates/" + branchName);

            client.ExecuteAsync(request, (resp, h) =>
            {
                GameThread.NextUpdate(x =>
                {
                    if (resp.StatusCode != System.Net.HttpStatusCode.OK)
                        callback(null);
                    else
                    {
                        var obj = JsonConvert.DeserializeObject<ApiUpdate[]>(resp.Content);
                        callback(obj);
                    }
                });
            });
        }
    }
}
