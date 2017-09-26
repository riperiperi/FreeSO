using FSO.Server.Api.Utils;
using FSO.Server.Protocol.Gluon.Model;
using System;
using System.Net;
using System.Net.Http;
using System.Web;
using System.Web.Http;

namespace FSO.Server.Api.Controllers.Admin
{
    public class AdminShardsController : ApiController
    {

        public HttpResponseMessage Get()
        {
            var api = Api.INSTANCE;
            api.DemandAdmin(Request);

            using (var db = api.DAFactory.Get())
            {
                var shards = db.Shards.All();
                return ApiResponse.Json(HttpStatusCode.OK, shards);
            }
        }

        [HttpPost]
        public HttpResponseMessage shutdown([FromBody] ShutdownModel sd)
        {
            var api = Api.INSTANCE;
            api.DemandAdmin(Request);

            ShutdownType type = ShutdownType.SHUTDOWN;
            if (sd.update) type = ShutdownType.UPDATE;
            else if (sd.restart) type = ShutdownType.RESTART;

            api.RequestShutdown((uint)sd.timeout, type);

            return ApiResponse.Json(HttpStatusCode.OK, true);
        }

        [HttpPost]
        public HttpResponseMessage announce([FromBody] AnnouncementModel an)
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);

            api.BroadcastMessage(an.sender, an.subject, an.message);

            return ApiResponse.Json(HttpStatusCode.OK, true);
        }
    }

    public class AnnouncementModel
    {
        public string sender;
        public string subject;
        public string message;
        public int[] shard_ids;
    }

    public class ShutdownModel
    {
        public int timeout;
        public bool restart;
        public bool update;
        public int[] shard_ids;
    }
}
