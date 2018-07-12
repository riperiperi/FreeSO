using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Web.Http;
using System.Runtime.Caching;
using FSO.Server.Api.Utils;
using FSO.Common.Enum;

namespace FSO.Server.Api.Controllers
{

    public static class MemoryCacher
    {
        public static object GetValue(string key)
        {
            MemoryCache memoryCache = MemoryCache.Default;
            return memoryCache.Get(key);
        }

        public static bool Add(string key, object value, DateTimeOffset absExpiration)
        {
            MemoryCache memoryCache = MemoryCache.Default;
            return memoryCache.Add(key, value, absExpiration);
        }

        public static void Delete(string key)
        {
            MemoryCache memoryCache = MemoryCache.Default;
            if (memoryCache.Contains(key))
            {
                memoryCache.Remove(key);
            }
        }
    }

    public class LotInfoController : ApiController
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


        public HttpResponseMessage Get(int shardid, uint id)
        {
            var dat = (byte[])MemoryCacher.GetValue("lt" + shardid + ":" + id);
            if (dat != null)
            {
                HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                response.Content = new ByteArrayContent(dat);
                response.Headers.CacheControl = new CacheControlHeaderValue()
                {
                    Public = true,
                    MaxAge = new TimeSpan(1, 0, 0),
                };
                response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
            }

            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lot = IDForLocation(shardid, id);
                if (lot == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

                FileStream stream;
                try
                {
                    var ndat = File.ReadAllBytes(Path.Combine(api.Config.NFSdir, "Lots/" + lot.Value.ToString("x8") + "/thumb.png"));
                    MemoryCacher.Add("lt" + shardid + ":" + id, ndat, DateTime.Now.Add(new TimeSpan(1, 0, 0)));

                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new ByteArrayContent(ndat);
                    response.Headers.CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = new TimeSpan(1, 0, 0),
                    };
                    response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");

                    return response;
                }
                catch (Exception e)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            }
        }

        [HttpGet]
        [Route("userapi/city/{shardid}/i{id}.json")]
        public HttpResponseMessage GetJSON(int shardid, uint id)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lot = da.Lots.GetByLocation(shardid, id);
                if (lot == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

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

        //moderation only functions, such as downloading lot state
        [HttpGet]
        [Route("userapi/city/{shardid}/{id}.fsov")]
        public HttpResponseMessage GetFSOV(int shardid, uint id)
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);

            using (var da = api.DAFactory.Get())
            {
                var lot = da.Lots.GetByLocation(shardid, id);
                if (lot == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

                FileStream stream;
                try
                {
                    var path = Path.Combine(api.Config.NFSdir, "Lots/" + lot.lot_id.ToString("x8") + "/state_" + lot.ring_backup_num.ToString() + ".fsov");
                    stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new StreamContent(stream);
                    /*response.Headers.CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = new TimeSpan(0, 15, 0),
                    };*/
                    response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                    return response;
                }
                catch (Exception e)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            }
        }

        [HttpGet]
        [Route("userapi/city/{shardid}/{id}.fsof")]
        public HttpResponseMessage GetFSOF(int shardid, uint id)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lot = IDForLocation(shardid, id);
                if (lot == null) return new HttpResponseMessage(HttpStatusCode.NotFound);

                FileStream stream;
                try
                {
                    var path = Path.Combine(api.Config.NFSdir, "Lots/" + lot.Value.ToString("x8") + "/thumb.fsof");
                    stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
                    HttpResponseMessage response = new HttpResponseMessage(HttpStatusCode.OK);
                    response.Content = new StreamContent(stream);
                    response.Headers.CacheControl = new CacheControlHeaderValue()
                    {
                        Public = true,
                        MaxAge = new TimeSpan(1, 0, 0),
                    };
                    response.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

                    return response;
                }
                catch (Exception e)
                {
                    return new HttpResponseMessage(HttpStatusCode.NotFound);
                }
            }
        }

        [HttpPost]
        [Route("userapi/city/{shardid}/uploadfacade/{id}")]
        public HttpResponseMessage UploadFacade(int shardid, uint id)
        {
            var api = Api.INSTANCE;
            api.DemandModerator(Request);

            if (!Request.Content.IsMimeMultipartContent())
                throw new HttpResponseException(HttpStatusCode.UnsupportedMediaType);

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
                    stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.Write);
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

    public class JSONLot
    {
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
}
