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
using FSO.Server.Database.DA.Avatars;

namespace FSO.Server.Api.Core.Controllers
{
    [EnableCors]
    [ApiController]
    public class AvatarInfoController : ControllerBase
    {
        //get the avatar by id
        [HttpGet]
        [Route("userapi/avatars/{avartarId}")]
        public IActionResult GetByID(uint avartarId)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var avatar = da.Avatars.Get(avartarId);
                if (avatar == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONAvatarError("Avatar not found"));

                var avatarJson = new JSONAvatar
                {
                    avatar_id = avatar.avatar_id,
                    shard_id = avatar.shard_id,
                    name = avatar.name,
                    gender = avatar.gender,
                    date = avatar.date,
                    description = avatar.description,
                    current_job = avatar.current_job,
                    mayor_nhood = avatar.mayor_nhood

                };

                return ApiResponse.Json(HttpStatusCode.OK, avatarJson);
            }
        }
        //gets all the avatars from one city
        [HttpGet]
        [Route("userapi/city/{shardId}/avatars/page/{pageNum}")]
        public IActionResult GetAll(int shardId,int pageNum)
        {
            var api = Api.INSTANCE;
            
            using (var da = api.DAFactory.Get())
            {
                pageNum = pageNum - 1;
                
                var avatars = da.Avatars.All(shardId);
                var avatarCount = avatars.Count();
                var totalPages = (avatars.Count() - 1)/100 + 1;
                avatars = avatars.Skip(pageNum * 100);
                avatars = avatars.Take(100);

                var pageAvatarsJson = new JSONAvatarsPage();
                pageAvatarsJson.total_avatars = avatarCount;
                pageAvatarsJson.page = pageNum + 1;
                pageAvatarsJson.total_pages = (int)totalPages;
                pageAvatarsJson.avatars_on_page = avatars.Count();

                if (pageNum < 0 || pageNum >= (int)totalPages) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONAvatarError("Page not found"));
                if (avatars == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONAvatarError("Avatar not found"));

                List<JSONAvatar> avatarJson = new List<JSONAvatar>();
                foreach (var avatar in avatars)
                {
                    avatarJson.Add(new JSONAvatar
                    {
                        avatar_id = avatar.avatar_id,
                        shard_id = avatar.shard_id,
                        name = avatar.name,
                        gender = avatar.gender,
                        date = avatar.date,
                        description = avatar.description,
                        current_job = avatar.current_job,
                        mayor_nhood = avatar.mayor_nhood
                    });

                }
                
                pageAvatarsJson.avatars = avatarJson;
                return ApiResponse.Json(HttpStatusCode.OK, pageAvatarsJson);
            }
        }
        //gets avatar by name
        [HttpGet]
        [Route("userapi/city/{shardId}/avatars/name/{name}")]
        public IActionResult GetByName(int shardId, string name)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var avatar = da.Avatars.SearchExact(shardId, name, 1).FirstOrDefault();
                if (avatar == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONAvatarError("Avatar not found"));

                var avatarJson = new JSONAvatar();
                var avatarById = da.Avatars.Get(avatar.avatar_id);
                if (avatarById == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONAvatarError("Avatar not found"));
                avatarJson = (new JSONAvatar
                {
                    avatar_id = avatarById.avatar_id,
                    shard_id = avatarById.shard_id,
                    name = avatarById.name,
                    gender = avatarById.gender,
                    date = avatarById.date,
                    description = avatarById.description,
                    current_job = avatarById.current_job,
                    mayor_nhood = avatarById.mayor_nhood
                });
                return ApiResponse.Json(HttpStatusCode.OK, avatarJson);
            }
        }
        //gets all the avatars that live in a specific neighbourhood
        [HttpGet]
        [Route("userapi/city/{shardId}/avatars/neighborhood/{nhoodId}")]
        public IActionResult GetByNhood(int shardId, uint nhoodId)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var lots = da.Lots.All(shardId).Where(x => x.neighborhood_id == nhoodId);
                if (lots == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONAvatarError("Lots not found"));

                List<JSONAvatar> avatarJson = new List<JSONAvatar>();
                foreach (var lot in lots)
                {
                    var roomies = da.Roommates.GetLotRoommates(lot.lot_id).Where(x => x.is_pending == 0).Select(x => x.avatar_id);
                    foreach (var roomie in roomies)
                    {
                        var roomieAvatar = da.Avatars.Get(roomie);
                        avatarJson.Add(new JSONAvatar
                        {
                            avatar_id = roomieAvatar.avatar_id,
                            shard_id = roomieAvatar.shard_id,
                            name = roomieAvatar.name,
                            gender = roomieAvatar.gender,
                            date = roomieAvatar.date,
                            description = roomieAvatar.description,
                            current_job = roomieAvatar.current_job,
                            mayor_nhood = roomieAvatar.mayor_nhood
                        });
                    }
                }
                var avatarsJson = new JSONAvatars();
                avatarsJson.avatars = avatarJson;
                return ApiResponse.Json(HttpStatusCode.OK, avatarsJson);
            }
        }
        //get all online Avatars
        [HttpGet]
        [Route("userapi/avatars/online")]
        public IActionResult GetOnline()
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var avatarStatus = da.AvatarClaims.GetAllActiveAvatars();
                if (avatarStatus == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONAvatarError("Avatars not found"));
                

                List<JSONAvatarSmall> avatarSmallJson = new List<JSONAvatarSmall>();

                foreach (var avatar in avatarStatus)
                {
                    uint location = 0;
                    if (avatar.privacy_mode == 0)
                    {
                        location = avatar.location;
                    }
                    avatarSmallJson.Add(new JSONAvatarSmall
                    {
                        avatar_id = avatar.avatar_id,
                        name = avatar.name,
                        privacy_mode = avatar.privacy_mode,
                        location = location
                    });

                }
                var avatarJson = new JSONAvatarOnline();
                avatarJson.avatars_online_count = avatarStatus.Count();
                avatarJson.avatars = avatarSmallJson;
                return ApiResponse.Json(HttpStatusCode.OK, avatarJson);
            }
        }
    }
    public class JSONAvatarError
    {
        public string error;
        public JSONAvatarError(string errorString)
        {
            error = errorString;
        }
    }
    public class JSONAvatarsPage
    {
        public int page { get; set; }
        public int total_pages { get; set; }
        public int total_avatars { get; set; }
        public int avatars_on_page { get; set; }
        public List<JSONAvatar> avatars { get; set; }
    }
    public class JSONAvatarOnline
    {
        public int avatars_online_count { get; set; }
        public List<JSONAvatarSmall> avatars { get; set; }
    }
    public class JSONAvatarSmall
    {
        public uint avatar_id { get; set; }
        public string name { get; set; }
        public byte privacy_mode { get; set; }
        public uint location { get; set; }
    }
    public class JSONAvatars
    {
        public List<JSONAvatar> avatars { get; set; }
    }
    public class JSONAvatar
    {
        public uint avatar_id { get; set; }
        public int shard_id { get; set; }
        public string name { get; set; }
        public DbAvatarGender gender { get; set; }
        public uint date { get; set; }
        public string description { get; set; }
        public ushort current_job { get; set; }
        public int? mayor_nhood { get; set; }
    }
}
