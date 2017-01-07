using FSO.Common.Utils;
using FSO.Server.Api.Utils;
using FSO.Server.Protocol.CitySelector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace FSO.Server.Api.Controllers
{
    public class ShardStatusController : ApiController
    {
        public HttpResponseMessage Get()
        {
            var api = Api.INSTANCE;

            var result = new XMLList<ShardStatusItem>("Shard-Status-List");
            var shards = api.Shards.All;
            foreach (var shard in shards)
            {
                result.Add(shard);
            }
            return ApiResponse.Xml(HttpStatusCode.OK, result);
        }
    }
}