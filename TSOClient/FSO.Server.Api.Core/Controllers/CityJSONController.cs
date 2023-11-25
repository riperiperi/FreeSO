using FSO.Server.Api.Core.Utils;
using FSO.Server.Common;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FSO.Server.Api.Core.Controllers
{
    [EnableCors]
    [ApiController]
    public class CityJSONController : ControllerBase
    {
        private static object ModelLock = new object { };
        private static CityInfoModel LastModel = new CityInfoModel();
        private static uint LastModelUpdate;

        [HttpGet]
        [Route("userapi/city/{shardid}/city.json")]
        public IActionResult Get(int shardid)
        {
            var api = Api.INSTANCE;

            var now = Epoch.Now;
            if (LastModelUpdate < now - 15)
            {
                LastModelUpdate = now;
                lock (ModelLock)
                {
                    LastModel = new CityInfoModel();
                    using (var da = api.DAFactory.Get())
                    {
                        var lots = da.Lots.AllLocations(shardid);
                        var lotstatus = da.LotClaims.AllLocations(shardid);
                        LastModel.reservedLots = lots.ConvertAll(x => x.location).ToArray();
                        LastModel.names = lots.ConvertAll(x => x.name).ToArray();
                        LastModel.activeLots = lotstatus.ConvertAll(x => x.location).ToArray();
                        LastModel.onlineCount = lotstatus.ConvertAll(x => x.active).ToArray();
                    }
                }
            }
            lock (ModelLock)
            {
                return ApiResponse.Json(HttpStatusCode.OK, LastModel);
            }
        }

        [HttpGet]
        [Route("userapi/city/thumbwork.json")]
        public IActionResult ThumbWork()
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);

            using (var da = api.DAFactory.Get())
            {
                var work = da.Lots.Get3DWork();
                if (work == null) return ApiResponse.Plain(HttpStatusCode.OK, "");
                else
                {
                    return ApiResponse.Json(HttpStatusCode.OK, work);
                }
            }
        }
    }

    class CityInfoModel
    {
        public string[] names;
        public uint[] reservedLots;
        public uint[] activeLots;
        public int[] onlineCount;
    }
}
