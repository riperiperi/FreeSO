using FSO.Server.Api.Core.Utils;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FSO.Server.Database.DA.Bulletin;

namespace FSO.Server.Api.Core.Controllers
{
    [EnableCors]
    [ApiController]
    public class BulletinInfoController : ControllerBase
    {
        [HttpGet("nhoodId")]
        [Route("userapi/neighborhood/{nhoodId}/bulletins")]
        public IActionResult GetByNhoodAndAfter(uint nhoodId, [FromQuery(Name = "after")]uint after)
        {
            var api = Api.INSTANCE;
            
            using (var da = api.DAFactory.Get())
            {
                if (after == null) after = 0;
                var bulletins = da.BulletinPosts.GetByNhoodId(nhoodId, after);
                if (bulletins == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONBulletinError("Bulletins not found"));

                List<JSONBulletin> bulletinJson = new List<JSONBulletin>();
                foreach (var bulletin in bulletins)
                {
                    bulletinJson.Add(new JSONBulletin
                    {
                        bulletin_id = bulletin.bulletin_id,
                        neighborhood_id = bulletin.neighborhood_id,
                        avatar_id = bulletin.avatar_id,
                        title = bulletin.title,
                        body = bulletin.body,
                        date = bulletin.date,
                        flags = bulletin.flags,
                        lot_id = bulletin.lot_id,
                        type = bulletin.type
                    });

                }
                var bulletinsJson = new JSONBulletins();
                bulletinsJson.bulletins = bulletinJson;
                return ApiResponse.Json(HttpStatusCode.OK, bulletinsJson);
            }
        }
        [HttpGet]
        [Route("userapi/neighborhood/{nhoodid}/bulletins/{bulletinId}")]
        public IActionResult GetByID(uint bulletinId)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var bulletin = da.BulletinPosts.Get(bulletinId);
                if (bulletin == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONBulletinError("Bulletin not found"));

                var bulletinJson = new JSONBulletin();
                bulletinJson = new JSONBulletin
                {
                    bulletin_id = bulletin.bulletin_id,
                    neighborhood_id = bulletin.neighborhood_id,
                    avatar_id = bulletin.avatar_id,
                    title = bulletin.title,
                    body = bulletin.body,
                    date = bulletin.date,
                    flags = bulletin.flags,
                    lot_id = bulletin.lot_id,
                    type = bulletin.type
                };
                return ApiResponse.Json(HttpStatusCode.OK, bulletinJson);
            }
        }
        [HttpGet]
        [Route("userapi/neighborhood/{nhoodId}/bulletins/type/{bulletinType}")]
        public IActionResult GetByNhoodAndType(uint nhoodId,DbBulletinType bulletinType)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var bulletins = da.BulletinPosts.GetByNhoodId(nhoodId, 0).Where(x => x.type == bulletinType);
                if (bulletins == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONBulletinError("Bulletins not found"));

                List<JSONBulletin> bulletinJson = new List<JSONBulletin>();
                foreach (var bulletin in bulletins)
                {
                    bulletinJson.Add(new JSONBulletin
                    {
                        bulletin_id = bulletin.bulletin_id,
                        neighborhood_id = bulletin.neighborhood_id,
                        avatar_id = bulletin.avatar_id,
                        title = bulletin.title,
                        body = bulletin.body,
                        date = bulletin.date,
                        flags = bulletin.flags,
                        lot_id = bulletin.lot_id,
                        type = bulletin.type
                    });

                }
                var bulletinsJson = new JSONBulletins();
                bulletinsJson.bulletins = bulletinJson;
                return ApiResponse.Json(HttpStatusCode.OK, bulletinsJson);
            }
        }
    }
    public class JSONBulletinError
    {
        public string error;
        public JSONBulletinError(string errorString)
        {
            error = errorString;
        }
    }
    public class JSONBulletins
    {
        public List<JSONBulletin> bulletins { get; set; }
    }
    public class JSONBulletin
    {
        public uint bulletin_id { get; set; }
        public int neighborhood_id { get; set; }
        public uint? avatar_id { get; set; }
        public string title { get; set; }
        public string body { get; set; }
        public uint date { get; set; }
        public uint flags { get; set; }
        public int? lot_id { get; set; }
        public DbBulletinType type { get; set; }
    }
}
