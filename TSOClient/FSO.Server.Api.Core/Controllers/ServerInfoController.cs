using FSO.Server.Api.Core.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA.Shards;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net;

namespace FSO.Server.Api.Core.Controllers
{
    [EnableCors]
    [ApiController]
    public class ServerInfoController : ControllerBase
    {
        private static object ModelLock = new object { };
        private static ServerInfoModel LastModel = new ServerInfoModel();
        private static uint LastModelUpdate;

        private static bool ShardUp(ShardStatus status)
        {
            return !(status == ShardStatus.Closed || status == ShardStatus.Down);
        }

        [HttpGet]
        [Route("userapi/status.json")]
        public IActionResult Get(int shardid)
        {
            var api = Api.INSTANCE;

            var now = Epoch.Now;
            if (LastModelUpdate < now - 15)
            {
                LastModelUpdate = now;
                lock (ModelLock)
                {
                    LastModel = new ServerInfoModel();
                    using (var da = api.DAFactory.Get())
                    {
                        var shards = da.Shards.All();
                        // TODO: only list shards for this server?
                        LastModel.shards = [.. shards.Where(shard => ShardUp(shard.status)).Select(shard => shard.shard_id)];
                        LastModel.name = api.Config.Name;

                        int onlineCount = 0;

                        foreach (int shardId in LastModel.shards)
                        {
                            var lotstatus = da.LotClaims.AllLocations(shardid);

                            onlineCount += lotstatus.Sum(x => x.active);
                        }

                        LastModel.onlineCount = onlineCount;
                        LastModel.versionInfo = api.Config.VersionInfoJson;
                    }
                }
            }

            lock (ModelLock)
            {
                return ApiResponse.Json(HttpStatusCode.OK, LastModel);
            }
        }
    }

    class ServerInfoModel
    {
        public string name;
        public int[] shards;
        public int onlineCount;
        public string versionInfo;
    }
}
