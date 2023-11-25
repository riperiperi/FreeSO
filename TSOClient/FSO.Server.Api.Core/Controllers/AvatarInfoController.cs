using FSO.Server.Api.Core.Utils;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using FSO.Server.Database.DA.Avatars;

namespace FSO.Server.Api.Core.Controllers
{
    [EnableCors]
    [ApiController]
    public class AvatarInfoController : ControllerBase
    {
        //get the avatars by user_id
        [Route("userapi/user/avatars")]
        public IActionResult GetByUser()
        {
            var api = Api.INSTANCE;
            var user = api.RequireAuthentication(Request);
            if (!user.Claims.Contains("userReadPermissions")) return ApiResponse.Json(HttpStatusCode.OK, new JSONAvatarError("No read premissions found."));

            using (var da = api.DAFactory.Get())
            {
                var avatars = da.Avatars.GetByUserId(user.UserID);
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
                var avatarsJson = new JSONAvatars();
                avatarsJson.avatars = avatarJson;
                return ApiResponse.Json(HttpStatusCode.OK, avatarsJson);
            }
        }
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
        //get the avatars by ids
        [Route("userapi/avatars")]
        public IActionResult GetByIDs([FromQuery(Name = "ids")]string idsString)
        {
            var api = Api.INSTANCE;
            try
            {
                uint[] ids = Array.ConvertAll(idsString.Split(","), uint.Parse);
                using (var da = api.DAFactory.Get())
                {
                    var avatars = da.Avatars.GetMultiple(ids);
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
                    var avatarsJson = new JSONAvatars();
                    avatarsJson.avatars = avatarJson;
                    return ApiResponse.Json(HttpStatusCode.OK, avatarsJson);
                }
            }
            catch
            {
                return ApiResponse.Json(HttpStatusCode.NotFound, new JSONAvatarError("Error during cast. (invalid_value)"));
            }
        }
        //gets all the avatars from one city
        [HttpGet]
        [Route("userapi/city/{shardId}/avatars/page/{pageNum}")]
        public IActionResult GetAll(int shardId,int pageNum, [FromQuery(Name = "avatars_on_page")]int perPage)
        {
            var api = Api.INSTANCE;
            if(perPage == 0)
            {
                perPage = 100;
            }
            if (perPage > 500) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONLotError("The max amount of avatars per page is 500"));
            using (var da = api.DAFactory.Get())
            {
                pageNum = pageNum - 1;
                
                var avatars = da.Avatars.AllByPage(shardId, pageNum * perPage, perPage,"avatar_id");
                var avatarCount = avatars.Total;
                var totalPages = (avatars.Total - 1)/perPage + 1;

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
                    if(lot.category != FSO.Common.Enum.LotCategory.community)
                    {
                        var roomies = da.Roommates.GetLotRoommates(lot.lot_id).Where(x => x.is_pending == 0).Select(x => x.avatar_id);
                        var avatars = da.Avatars.GetMultiple(roomies.ToArray());
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
        public IActionResult GetOnline([FromQuery(Name = "compact")]bool compact)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                List<JSONAvatarSmall> avatarSmallJson = new List<JSONAvatarSmall>();
                var avatarJson = new JSONAvatarOnline();
                if (compact)
                {
                    var avatarStatus = da.AvatarClaims.GetAllActiveAvatarsCount();
                    if (avatarStatus == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONAvatarError("ammount not found"));
                    avatarJson.avatars_online_count = avatarStatus;
                }
                
                
                if (!compact)
                {
                    var avatarStatus = da.AvatarClaims.GetAllActiveAvatars();
                    if (avatarStatus == null) return ApiResponse.Json(HttpStatusCode.NotFound, new JSONAvatarError("Avatars not found"));

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
                    avatarJson.avatars_online_count = avatarStatus.Count();
                }
                
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
        public int? avatars_online_count { get; set; }
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
