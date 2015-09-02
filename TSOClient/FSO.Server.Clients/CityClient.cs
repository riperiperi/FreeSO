using FSO.Common.Utils;
using FSO.Server.Clients.Framework;
using FSO.Server.Protocol.CitySelector;
using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Clients
{
    public class CityClient : AbstractHttpClient
    {
        public CityClient(string baseUrl) : base(baseUrl) {
        }

        public ShardSelectorServletResponse ShardSelectorServlet(ShardSelectorServletRequest input)
        {
            var client = Client();

            var request = new RestRequest("cityselector/app/ShardSelectorServlet")
                            .AddQueryParameter("shardName", input.ShardName)
                            .AddQueryParameter("avatarId", input.AvatarID);
            
            var response = client.Execute(request);
            if(response.StatusCode != System.Net.HttpStatusCode.OK){
                throw new Exception("Unknown error during ShardSelectorServlet");
            }

            return XMLUtils.Parse<ShardSelectorServletResponse>(response.Content);
        }


        public InitialConnectServletResult InitialConnectServlet(InitialConnectServletRequest input)
        {
            var client = Client();

            var request = new RestRequest("cityselector/app/InitialConnectServlet")
                            .AddQueryParameter("ticket", input.Ticket)
                            .AddQueryParameter("version", input.Version);

            var response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Unknown error during InitialConnectServlet");
            }

            return XMLUtils.Parse<InitialConnectServletResult>(response.Content);
        }

        public List<AvatarData> AvatarDataServlet()
        {
            var client = Client();

            var request = new RestRequest("cityselector/app/AvatarDataServlet");

            var response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Unknown error during AvatarDataServlet");
            }

            List<AvatarData> result = (List<AvatarData>)XMLUtils.Parse<XMLList<AvatarData>>(response.Content);
            return result;
        }


        public List<ShardStatusItem> ShardStatus()
        {
            var client = Client();

            var request = new RestRequest("cityselector/shard-status.jsp");

            var response = client.Execute(request);
            if (response.StatusCode != System.Net.HttpStatusCode.OK)
            {
                throw new Exception("Unknown error during ShardStatus");
            }

            List<ShardStatusItem> result = (List<ShardStatusItem>)XMLUtils.Parse<XMLList<ShardStatusItem>>(response.Content);
            return result;
        }
    }
}
