using FSO.Server.Api.Core.Utils;
using FSO.Server.Common;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using FSO.Server.Database.DA.Bulletin;

namespace FSO.Server.Api.Core.Controllers
{
    [EnableCors]
    [ApiController]
    public class BulletinInfoController : ControllerBase
    {
        [HttpGet]
        [Route("userapi/bulletins/neighborhood/{nhoodid}.json")]
        public IActionResult GetByNhood(uint nhoodid)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var Bulletins = da.BulletinPosts.GetByNhoodId(nhoodid,0);
                if (Bulletins == null)
                {
                    var JSONError = new JSONBulletinError();
                    JSONError.Error = "Bulletins not found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                List<JSONBulletin> BulletinJSON = new List<JSONBulletin>();
                foreach (var Bulletin in Bulletins)
                {
                    BulletinJSON.Add(new JSONBulletin
                    {
                        Bulletin_ID = Bulletin.bulletin_id,
                        Neighborhood_ID = Bulletin.neighborhood_id,
                        Avatar_ID = Bulletin.avatar_id,
                        Title = Bulletin.title,
                        Body = Bulletin.body,
                        Date = Bulletin.date,
                        Flags = Bulletin.flags,
                        Lot_ID = Bulletin.lot_id,
                        Type = Bulletin.type
                    });

                }
                var BulletinsJSON = new JSONBulletins();
                BulletinsJSON.Bulletins = BulletinJSON;
                return ApiResponse.Json(HttpStatusCode.OK, BulletinsJSON);
            }
        }
        [HttpGet]
        [Route("userapi/bulletins/neighborhood/{nhoodid}/after/{timestamp}.json")]
        public IActionResult GetByNhoodAndAfter(uint nhoodid, uint timestamp)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var Bulletins = da.BulletinPosts.GetByNhoodId(nhoodid, timestamp);
                if (Bulletins == null)
                {
                    var JSONError = new JSONBulletinError();
                    JSONError.Error = "Bulletins not found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                List<JSONBulletin> BulletinJSON = new List<JSONBulletin>();
                foreach (var Bulletin in Bulletins)
                {
                    BulletinJSON.Add(new JSONBulletin
                    {
                        Bulletin_ID = Bulletin.bulletin_id,
                        Neighborhood_ID = Bulletin.neighborhood_id,
                        Avatar_ID = Bulletin.avatar_id,
                        Title = Bulletin.title,
                        Body = Bulletin.body,
                        Date = Bulletin.date,
                        Flags = Bulletin.flags,
                        Lot_ID = Bulletin.lot_id,
                        Type = Bulletin.type
                    });

                }
                var BulletinsJSON = new JSONBulletins();
                BulletinsJSON.Bulletins = BulletinJSON;
                return ApiResponse.Json(HttpStatusCode.OK, BulletinsJSON);
            }
        }
        [HttpGet]
        [Route("userapi/bulletins/id/{bulletinid}.json")]
        public IActionResult GetByID(uint bulletinid)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var Bulletin = da.BulletinPosts.Get(bulletinid);
                if (Bulletin == null)
                {
                    var JSONError = new JSONBulletinError();
                    JSONError.Error = "Bulletin not found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                var BulletinJSON = new JSONBulletin();
                BulletinJSON = new JSONBulletin
                {
                    Bulletin_ID = Bulletin.bulletin_id,
                    Neighborhood_ID = Bulletin.neighborhood_id,
                    Avatar_ID = Bulletin.avatar_id,
                    Title = Bulletin.title,
                    Body = Bulletin.body,
                    Date = Bulletin.date,
                    Flags = Bulletin.flags,
                    Lot_ID = Bulletin.lot_id,
                    Type = Bulletin.type
                };
                return ApiResponse.Json(HttpStatusCode.OK, BulletinJSON);
            }
        }
        [HttpGet]
        [Route("userapi/bulletins/neighborhood/{nhoodid}/type/{bulletintype}.json")]
        public IActionResult GetByNhoodAndType(uint nhoodid,DbBulletinType bulletintype)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var Bulletins = da.BulletinPosts.GetByNhoodId(nhoodid, 0).Where(x => x.type == bulletintype);
                if (Bulletins == null)
                {
                    var JSONError = new JSONBulletinError();
                    JSONError.Error = "Bulletins not found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                List<JSONBulletin> BulletinJSON = new List<JSONBulletin>();
                foreach (var Bulletin in Bulletins)
                {
                    BulletinJSON.Add(new JSONBulletin
                    {
                        Bulletin_ID = Bulletin.bulletin_id,
                        Neighborhood_ID = Bulletin.neighborhood_id,
                        Avatar_ID = Bulletin.avatar_id,
                        Title = Bulletin.title,
                        Body = Bulletin.body,
                        Date = Bulletin.date,
                        Flags = Bulletin.flags,
                        Lot_ID = Bulletin.lot_id,
                        Type = Bulletin.type
                    });

                }
                var BulletinsJSON = new JSONBulletins();
                BulletinsJSON.Bulletins = BulletinJSON;
                return ApiResponse.Json(HttpStatusCode.OK, BulletinsJSON);
            }
        }
    }
    public class JSONBulletinError
    {
        public string Error { get; set; }
    }
    public class JSONBulletins
    {
        public List<JSONBulletin> Bulletins { get; set; }
    }
    public class JSONBulletin
    {
        public uint Bulletin_ID { get; set; }
        public int Neighborhood_ID { get; set; }
        public uint? Avatar_ID { get; set; }
        public string Title { get; set; }
        public string Body { get; set; }
        public uint Date { get; set; }
        public uint Flags { get; set; }
        public int? Lot_ID { get; set; }
        public DbBulletinType Type { get; set; }
    }
}
