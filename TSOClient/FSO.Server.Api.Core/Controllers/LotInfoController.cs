using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using FSO.Server.Api.Core.Utils;
using FSO.Common.Enum;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Cors;

namespace FSO.Server.Api.Core.Controllers
{

    public static class MemoryCacher
    {
        public static MemoryCache Default = new MemoryCache(new MemoryCacheOptions());
        public static object GetValue(string key)
        {
            MemoryCache memoryCache = Default;
            return memoryCache.Get(key);
        }

        public static bool Add(string key, object value, DateTimeOffset absExpiration)
        {
            MemoryCache memoryCache = Default;
            return memoryCache.Set(key, value, absExpiration) == value;
        }

        public static void Delete(string key)
        {
            MemoryCache memoryCache = Default;
            memoryCache.Remove(key);
        }
    }

    [EnableCors]
    [ApiController]
    public class LotInfoController : ControllerBase
    {
        public static ConcurrentDictionary<int, ShardLocationCache> LotLocationCache = new ConcurrentDictionary<int, ShardLocationCache>();

        public static int? IDForLocation(int shardid, uint loc)
        {
            var api = Api.INSTANCE;
            var locToID = LotLocationCache.GetOrAdd(shardid, (ikey) =>
            {
                using (var da = api.DAFactory.Get())
                {
                    return new ShardLocationCache(
                        new ConcurrentDictionary<uint, int>(da.Lots.All(ikey).Select(x => new KeyValuePair<uint, int>(x.location, x.lot_id)))
                        );
                }
            });
            if (DateTime.UtcNow - locToID.CreateTime > TimeSpan.FromMinutes(15))
            {
                ShardLocationCache removed;
                LotLocationCache.TryRemove(shardid, out removed);
            }

            try
            {
                return locToID.Dict.GetOrAdd(loc, (ikey) =>
                {
                    using (var da = api.DAFactory.Get())
                    {
                        return da.Lots.GetByLocation(shardid, ikey).lot_id;
                    }
                });
            } catch (NullReferenceException e)
            {
                return null;
            }
        }

        [HttpGet]
        [Route("userapi/city/{shardid}/{id}.png")]
        [ResponseCache(Duration = 60 * 60, Location = ResponseCacheLocation.Any)]
        public IActionResult Get(int shardid, uint id)
        {
            var dat = (byte[])MemoryCacher.GetValue("lt" + shardid + ":" + id);
            if (dat != null)
            {
                return File(dat, "image/png");
            }

            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lot = IDForLocation(shardid, id);
                if (lot == null) return NotFound();

                FileStream stream;
                try
                {
                    var ndat = System.IO.File.ReadAllBytes(Path.Combine(api.Config.NFSdir, "Lots/" + lot.Value.ToString("x8") + "/thumb.png"));
                    MemoryCacher.Add("lt" + shardid + ":" + id, ndat, DateTime.Now.Add(new TimeSpan(1, 0, 0)));

                    return File(ndat, "image/png");
                }
                catch (Exception e)
                {
                    return NotFound();
                }
            }
        }

        [HttpGet]
        [Route("userapi/city/{shardid}/i{id}.json")]
        public IActionResult GetJSON(int shardid, uint id)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lot = da.Lots.GetByLocation(shardid, id);
                if (lot == null) return NotFound();

                var roomies = da.Roommates.GetLotRoommates(lot.lot_id).Where(x => x.is_pending == 0).Select(x => x.avatar_id).ToArray();

                var jlot = new JSONLot
                {
                    admit_mode = lot.admit_mode,
                    category = lot.category,
                    created_date = lot.created_date,
                    description = lot.description,
                    location = lot.location,
                    name = lot.name,
                    neighborhood_id = lot.neighborhood_id,
                    owner_id = lot.owner_id,
                    shard_id = lot.shard_id,
                    skill_mode = lot.skill_mode,
                    roommates = roomies
                };

                return ApiResponse.Json(HttpStatusCode.OK, jlot);
            }
        }

        //New user API calls might replace old once later
        //get lot information by location
        [HttpGet]
        [Route("userapi/lots/{lotId}")]
        public IActionResult GetByID(int lotId)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lot = da.Lots.Get(lotId);
                if (lot == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("Lot not found"));
                var roomies = da.Roommates.GetLotRoommates(lot.lot_id).Where(x => x.is_pending == 0).Select(x => x.avatar_id).ToArray();

                var lotJson = new JSONLot
                {
                    admit_mode = lot.admit_mode,
                    category = lot.category,
                    created_date = lot.created_date,
                    description = lot.description,
                    location = lot.location,
                    name = lot.name,
                    neighborhood_id = lot.neighborhood_id,
                    owner_id = lot.owner_id,
                    shard_id = lot.shard_id,
                    skill_mode = lot.skill_mode,
                    roommates = roomies,
                    lot_id = lot.lot_id
                };

                return ApiResponse.Json(HttpStatusCode.OK, lotJson);
            }
        }
        //get the lots by ids
        [Route("userapi/lots")]
        public IActionResult GetByIDs([FromQuery(Name = "ids")]string idsString)
        {
            var api = Api.INSTANCE;
            try
            {
                int[] ids = Array.ConvertAll(idsString.Split(","), int.Parse);
                using (var da = api.DAFactory.Get())
                {
                    var lots = da.Lots.GetMultiple(ids);
                    if (lots == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("Lot not found"));

                    List<JSONLot> lotJson = new List<JSONLot>();
                    foreach (var lot in lots)
                    {
                        var roomies = da.Roommates.GetLotRoommates(lot.lot_id).Where(x => x.is_pending == 0).Select(x => x.avatar_id).ToArray();
                        lotJson.Add(new JSONLot
                        {
                            admit_mode = lot.admit_mode,
                            category = lot.category,
                            created_date = lot.created_date,
                            description = lot.description,
                            location = lot.location,
                            name = lot.name,
                            neighborhood_id = lot.neighborhood_id,
                            owner_id = lot.owner_id,
                            shard_id = lot.shard_id,
                            skill_mode = lot.skill_mode,
                            roommates = roomies,
                            lot_id = lot.lot_id
                        });
                    }
                    var lotsJson = new JSONLots();
                    lotsJson.lots = lotJson;
                    return ApiResponse.Json(HttpStatusCode.OK, lotsJson);
                }
            }
            catch
            {
                return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("Error during cast. (invalid_value)"));
            }
        }
        //gets all the lots from one city
        [HttpGet]
        [Route("userapi/city/{shardId}/lots/page/{pageNum}")]
        public IActionResult GetAll(int shardId, int pageNum, [FromQuery(Name = "lots_on_page")]int perPage)
        {
            var api = Api.INSTANCE;
            if (perPage == 0)
            {
                perPage = 100;
            }
            if (perPage > 500) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("The max amount of lots per page is 500"));
            using (var da = api.DAFactory.Get())
            {
                pageNum = pageNum - 1;

                var lots = da.Lots.AllByPage(shardId, pageNum * perPage, perPage,"lot_id");
                var lotCount = lots.Total;
                var totalPages = (lots.Total - 1) / perPage + 1;
                
                var pageLotsJson = new JSONLotsPage();
                pageLotsJson.total_lots = lotCount;
                pageLotsJson.page = pageNum + 1;
                pageLotsJson.total_pages = (int)totalPages;
                pageLotsJson.lots_on_page = lots.Count();

                if (pageNum < 0 || pageNum >= (int)totalPages) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("Page not found"));
                if (lots == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("Lots not found"));

                List<JSONLotSmall> lotJson = new List<JSONLotSmall>();
                foreach (var lot in lots)
                {
                    lotJson.Add(new JSONLotSmall
                    {
                        location = lot.location,
                        name = lot.name,
                        description = lot.description,
                        category = lot.category,
                        admit_mode = lot.admit_mode,
                        neighborhood_id = lot.neighborhood_id,
                        lot_id = lot.lot_id
                    });
                }

                pageLotsJson.lots = lotJson;
                return ApiResponse.Json(HttpStatusCode.OK, pageLotsJson);
            }
        }
        //get lot information by location
        [HttpGet]
        [Route("userapi/city/{shardId}/lots/location/{locationId}")]
        public IActionResult GetByLocation(int shardId, uint locationId)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lot = da.Lots.GetByLocation(shardId, locationId);
                if (lot == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("Lot not found"));

                var roomies = da.Roommates.GetLotRoommates(lot.lot_id).Where(x => x.is_pending == 0).Select(x => x.avatar_id).ToArray();

                var LotJSON = new JSONLot
                {
                    admit_mode = lot.admit_mode,
                    category = lot.category,
                    created_date = lot.created_date,
                    description = lot.description,
                    location = lot.location,
                    name = lot.name,
                    neighborhood_id = lot.neighborhood_id,
                    owner_id = lot.owner_id,
                    shard_id = lot.shard_id,
                    skill_mode = lot.skill_mode,
                    roommates = roomies,
                    lot_id = lot.lot_id
                };

                return ApiResponse.Json(HttpStatusCode.OK, LotJSON);
            }
        }
        //get lot information By neighbourhood
        [HttpGet]
        [Route("userapi/city/{shardId}/lots/neighborhood/{nhoodId}")]
        public IActionResult GetByNhood(int shardId, uint nhoodId)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lots = da.Lots.All(shardId).Where(x => x.neighborhood_id == nhoodId);
                if (lots == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("lots not found"));
                List<JSONLotSmall> lotJson = new List<JSONLotSmall>();
                foreach (var lot in lots)
                {
                    lotJson.Add(new JSONLotSmall
                    {
                        location = lot.location,
                        name = lot.name,
                        description = lot.description,
                        category = lot.category,
                        admit_mode = lot.admit_mode,
                        neighborhood_id = lot.neighborhood_id,
                        lot_id = lot.lot_id
                    });
                }
                var lotsJson = new JSONLotsSmall();
                lotsJson.lots = lotJson;
                return ApiResponse.Json(HttpStatusCode.OK, lotsJson);
            }
        }
        //get lot information by name
        [HttpGet]
        [Route("userapi/city/{shardId}/lots/name/{lotName}")]
        public IActionResult GetByName(int shardId, string lotName)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lot = da.Lots.GetByName(shardId, lotName);
                if (lot == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("Lot not found"));

                var roomies = da.Roommates.GetLotRoommates(lot.lot_id).Where(x => x.is_pending == 0).Select(x => x.avatar_id).ToArray();

                var lotJson = new JSONLot
                {
                    lot_id = lot.lot_id,
                    admit_mode = lot.admit_mode,
                    category = lot.category,
                    created_date = lot.created_date,
                    description = lot.description,
                    location = lot.location,
                    name = lot.name,
                    neighborhood_id = lot.neighborhood_id,
                    owner_id = lot.owner_id,
                    shard_id = lot.shard_id,
                    skill_mode = lot.skill_mode,
                    roommates = roomies
                };

                return ApiResponse.Json(HttpStatusCode.OK, lotJson);
            }
        }
        //get online lots
        [HttpGet]
        [Route("userapi/city/{shardId}/lots/online")]
        public IActionResult GetOnline(int shardId, [FromQuery(Name = "compact")]bool compact)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                List<JSONLotSmall> lotSmallJson = new List<JSONLotSmall>();
                var lotsOnlineJson = new JSONLotsOnline();
                
                if (!compact)
                {
                    var activeLots = da.LotClaims.AllActiveLots(shardId);
                    if (activeLots == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("Lots not found"));
                    var totalAvatars = 0;
                    foreach (var lot in activeLots)
                    {
                        lotSmallJson.Add(new JSONLotSmall
                        {
                            location = lot.location,
                            name = lot.name,
                            description = lot.description,
                            category = lot.category,
                            admit_mode = lot.admit_mode,
                            neighborhood_id = lot.neighborhood_id,
                            avatars_in_lot = lot.active,
                            lot_id = lot.lot_id
                        });
                        totalAvatars += lot.active;
                    }
                    lotsOnlineJson.total_lots_online = activeLots.Count();
                    lotsOnlineJson.total_avatars_in_lots_online = totalAvatars;
                }
                else
                {
                    var activeLots = da.LotClaims.AllLocations(shardId);
                    if (activeLots == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("Lots not found"));
                    var totalAvatars = 0;
                    foreach (var lot in activeLots)
                    {
                        totalAvatars += lot.active;
                    }

                    lotsOnlineJson.total_lots_online = activeLots.Count();
                    lotsOnlineJson.total_avatars_in_lots_online = totalAvatars;
                }

                lotsOnlineJson.lots = lotSmallJson;
                return ApiResponse.Json(HttpStatusCode.OK, lotsOnlineJson);
            }
        }
        //get Top-100 lots by category
        [HttpGet]
        [Route("userapi/city/{shardId}/lots/top100/category/{lotCategory}")]
        public IActionResult GetTop100ByCategory(int shardId, LotCategory lotCategory)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lots = da.LotTop100.GetByCategory(shardId, lotCategory).Take(100);
                if (lots == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("Top100 lots not found"));

                List<JSONTop100Lot> top100Lots = new List<JSONTop100Lot>();
                foreach (var top100Lot in lots)
                {
                    top100Lots.Add(new JSONTop100Lot
                    {
                        category = top100Lot.category,
                        rank = top100Lot.rank,
                        shard_id = top100Lot.shard_id,
                        lot_location = top100Lot.lot_location,
                        lot_name = top100Lot.lot_name,
                        lot_id = top100Lot.lot_id
                    });
                }
                var top100Json = new JSONTop100Lots();
                top100Json.lots = top100Lots;
                return ApiResponse.Json(HttpStatusCode.OK, top100Json);
            }
        }
        //get Top-100 lots by shard
        [HttpGet]
        [Route("userapi/city/{shardId}/lots/top100/all")]
        public IActionResult GetTop100ByShard(int shardId)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lots = da.LotTop100.GetAllByShard(shardId);
                if (lots == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("Lots not found"));

                List<JSONTop100Lot> top100Lots = new List<JSONTop100Lot>();
                foreach (var top100Lot in lots)
                {
                    top100Lots.Add(new JSONTop100Lot
                    {
                        category = top100Lot.category,
                        rank = top100Lot.rank,
                        shard_id = top100Lot.shard_id,
                        lot_location = top100Lot.lot_location,
                        lot_name = top100Lot.lot_name,
                        lot_id = top100Lot.lot_id
                    });
                }
                var top100Json = new JSONTop100Lots();
                top100Json.lots = top100Lots;
                return ApiResponse.Json(HttpStatusCode.OK, top100Json);
            }
        }

        //moderation only functions, such as downloading lot state
        [HttpGet]
        [Route("userapi/city/{shardid}/{id}.fsov")]
        public IActionResult GetFSOV(int shardid, uint id)
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);

            using (var da = api.DAFactory.Get())
            {
                var lot = da.Lots.GetByLocation(shardid, id);
                if (lot == null) return NotFound();

                FileStream stream;
                try
                {
                    var path = Path.Combine(api.Config.NFSdir, "Lots/" + lot.lot_id.ToString("x8") + "/state_" + lot.ring_backup_num.ToString() + ".fsov");
                    

                    stream = System.IO.File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    return File(stream, "application/octet-stream");
                }
                catch (Exception e)
                {
                    return NotFound();
                }
            }
        }

        [HttpGet]
        [Route("userapi/city/{shardid}/{id}.fsof")]
        [ResponseCache(Duration = 60 * 60, Location = ResponseCacheLocation.Any)]
        public IActionResult GetFSOF(int shardid, uint id)
        {
            var dat = (byte[])MemoryCacher.GetValue("lf" + shardid + ":" + id);
            if (dat != null)
            {
                return File(dat, "application/octet-stream");
            }

            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lot = IDForLocation(shardid, id);
                if (lot == null) return NotFound();

                FileStream stream;
                try
                {
                    var path = Path.Combine(api.Config.NFSdir, "Lots/" + lot.Value.ToString("x8") + "/thumb.fsof");

                    var ndat = System.IO.File.ReadAllBytes(path);
                    MemoryCacher.Add("lf" + shardid + ":" + id, ndat, DateTime.Now.Add(new TimeSpan(1, 0, 0)));
                    return File(ndat, "application/octet-stream");
                }
                catch (Exception e)
                {
                    return NotFound();
                }
            }
        }

        [HttpPost]
        [Route("userapi/city/{shardid}/uploadfacade/{id}")]
        public IActionResult UploadFacade(int shardid, uint id, List<IFormFile> files)
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);

            if (files == null)
                return NotFound();

            byte[] data = null;
            foreach (var file in files)
            {
                var filename = file.FileName.Trim('\"');
                using (var memoryStream = new MemoryStream())
                {
                    file.CopyTo(memoryStream);
                    data = memoryStream.ToArray();
                }
            }

            if (data == null) return NotFound();

            using (var da = api.DAFactory.Get())
            {
                var lot = da.Lots.GetByLocation(shardid, id);
                if (lot == null) return NotFound();

                FileStream stream;
                try
                {
                    var path = Path.Combine(api.Config.NFSdir, "Lots/" + lot.lot_id.ToString("x8") + "/thumb.fsof");
                    stream = System.IO.File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Write);
                    stream.Write(data, 0, data.Length);
                    return Ok();
                }
                catch (Exception e)
                {
                    return NotFound();
                }
            }

            /*
            var api = Api.INSTANCE;
            api.DemandModerator(Request);

            if (!Request.Content.IsMimeMultipartContent())
                return new HttpResponseMessage(HttpStatusCode.UnsupportedMediaType);

            var provider = new MultipartMemoryStreamProvider();
            var files = Request.Content.ReadAsMultipartAsync(provider).Result;

            byte[] data = null;
            foreach (var file in provider.Contents)
            {
                var filename = file.Headers.ContentDisposition.FileName.Trim('\"');
                data = file.ReadAsByteArrayAsync().Result;
            }

            if (data == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

            using (var da = api.DAFactory.Get())
            {
                var lot = da.Lots.GetByLocation(shardid, id);
                if (lot == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

                FileStream stream;
                try
                {
                    var path = Path.Combine(api.Config.NFSdir, "Lots/" + lot.lot_id.ToString("x8") + "/thumb.fsof");
                    stream = System.IO.File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Write);
                    stream.Write(data, 0, data.Length);

                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new StringContent("", Encoding.UTF8, "text/plain");
                    return response;
                }
                catch (Exception e)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            }
            */
        }
    }


    public class ShardLocationCache
    {
        public ConcurrentDictionary<uint, int> Dict = new ConcurrentDictionary<uint, int>();
        public DateTime CreateTime = DateTime.UtcNow;

        public ShardLocationCache(ConcurrentDictionary<uint, int> dict)
        {
            Dict = dict;
        }
    }
    public class JSONLotError
    {
        public string error;
        public JSONLotError(string errorString)
        {
            error = errorString;
        }
    }
    public class JSONLotsSmall
    {
        public List<JSONLotSmall> lots { get; set; }
    }
    public class JSONLots
    {
        public List<JSONLot> lots { get; set; }
    }
    public class JSONLotsPage
    {
        public int page { get; set; }
        public int total_pages { get; set; }
        public int total_lots { get; set; }
        public int lots_on_page { get; set; }
        public List<JSONLotSmall> lots { get; set; }
    }
    public class JSONLotsOnline
    {
        public int total_lots_online { get; set; }
        public int total_avatars_in_lots_online { get; set; }
        public List<JSONLotSmall> lots { get; set; }
    }
    public class JSONLotSmall
    {
        public int lot_id { get; set; }
        public uint location { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public LotCategory category { get; set; }
        public uint admit_mode { get; set; }
        public uint neighborhood_id { get; set; }
        public int avatars_in_lot { get; set; }
    }
    public class JSONLot
    {
        public int lot_id { get; set; }
        public int shard_id { get; set; }
        public uint? owner_id { get; set; }
        public uint[] roommates { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public uint location { get; set; }
        public uint neighborhood_id { get; set; }
        public uint created_date { get; set; }
        public LotCategory category { get; set; }
        public byte skill_mode { get; set; }
        public byte admit_mode { get; set; }
    }
    public class JSONTop100Lots
    {
        public List<JSONTop100Lot> lots { get; set; }
    }
    public class JSONTop100Lot
    {
        public LotCategory category { get; set; }
        public byte rank { get; set; }
        public int shard_id { get; set; }
        public string lot_name { get; set; }
        public uint? lot_location { get; set; }
        public int? lot_id { get; set; }
    }
}
