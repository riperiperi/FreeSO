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
        //gets all the avatars from one city
        [HttpGet]
        [Route("userapi/city/{shardid}/avatars/all/page/{pagenum}.json")]
        public IActionResult GetAll(int shardid,int pagenum)
        {
            var api = Api.INSTANCE;
            
            using (var da = api.DAFactory.Get())
            {
                pagenum = pagenum - 1;
                
                var Avatar = da.Avatars.All(shardid);
                var Avatar_Count = Avatar.Count();
                var Total_Pages = Math.Ceiling((decimal)Avatar.Count()/100);
                Avatar = Avatar.Skip(pagenum * 100);
                Avatar = Avatar.Take(100);

                var PageAvatarsJSON = new JSONAvatarsPage();
                PageAvatarsJSON.Total_Avatars = Avatar_Count;
                PageAvatarsJSON.Page = pagenum + 1;
                PageAvatarsJSON.Total_Pages = (int)Total_Pages;
                PageAvatarsJSON.Avatars_On_Page = Avatar.Count();

                if (pagenum < 0 || pagenum >= (int)Total_Pages)
                {
                    var JSONError = new JSONAvatarError();
                    JSONError.Error = "Sorry page not found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }
                if (Avatar == null)
                {
                    var JSONError = new JSONAvatarError();
                    JSONError.Error = "No avatars found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                List<JSONAvatar> AvatarJSON = new List<JSONAvatar>();
                foreach (var avatar in Avatar)
                {
                    AvatarJSON.Add(new JSONAvatar
                    {
                        Avatar_ID = avatar.avatar_id,
                        Shard_ID = avatar.shard_id,
                        Name = avatar.name,
                        Gender = avatar.gender,
                        Date = avatar.date,
                        Description = avatar.description,
                        Current_Job = avatar.current_job,
                        Mayor_Nhood = avatar.mayor_nhood
                    });

                }
                
                PageAvatarsJSON.Avatars = AvatarJSON;
                return ApiResponse.Json(HttpStatusCode.OK, PageAvatarsJSON);
            }
        }
        //gets avatar by name
        [HttpGet]
        [Route("userapi/city/{shardid}/avatars/name/{name}.json")]
        public IActionResult GetByName(int shardid, string name)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var Avatar = da.Avatars.SearchExact(shardid, name, 1).FirstOrDefault();
                if (Avatar == null)
                {
                    var JSONError = new JSONAvatarError();
                    JSONError.Error = "No avatar found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                var AvatarJSON = new JSONAvatar();
                var avatarById = da.Avatars.Get(Avatar.avatar_id);
                if (avatarById == null)
                {
                    return ApiResponse.Json(HttpStatusCode.NotFound, "avatar not found");
                }
                AvatarJSON = (new JSONAvatar
                {
                    Avatar_ID = avatarById.avatar_id,
                    Shard_ID = avatarById.shard_id,
                    Name = avatarById.name,
                    Gender = avatarById.gender,
                    Date = avatarById.date,
                    Description = avatarById.description,
                    Current_Job = avatarById.current_job,
                    Mayor_Nhood = avatarById.mayor_nhood
                });
                return ApiResponse.Json(HttpStatusCode.OK, AvatarJSON);
            }
        }
        //gets all the avatars that live in a specific neighbourhood
        [HttpGet]
        [Route("userapi/city/{shardid}/avatars/neighborhood/{nhoodid}.json")]
        public IActionResult GetByNhood(int shardid, uint nhoodid)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var Lots = da.Lots.All(shardid).Where(x => x.neighborhood_id == nhoodid);
                if (Lots == null)
                {
                    var JSONError = new JSONAvatarError();
                    JSONError.Error = "No avatars found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }
                List<JSONAvatar> AvatarJSON = new List<JSONAvatar>();
                foreach (var lot in Lots)
                {
                    var Roomies = da.Roommates.GetLotRoommates(lot.lot_id).Where(x => x.is_pending == 0).Select(x => x.avatar_id);
                    foreach (var roomie in Roomies)
                    {
                        var roomieAvatar = da.Avatars.Get(roomie);
                        AvatarJSON.Add(new JSONAvatar
                        {
                            Avatar_ID = roomieAvatar.avatar_id,
                            Shard_ID = roomieAvatar.shard_id,
                            Name = roomieAvatar.name,
                            Gender = roomieAvatar.gender,
                            Date = roomieAvatar.date,
                            Description = roomieAvatar.description,
                            Current_Job = roomieAvatar.current_job,
                            Mayor_Nhood = roomieAvatar.mayor_nhood
                        });
                    }
                }
                var AvatarsJSON = new JSONAvatars();
                AvatarsJSON.Avatars = AvatarJSON;
                return ApiResponse.Json(HttpStatusCode.OK, AvatarsJSON);
            }
        }
        //get the avatar by id
        [HttpGet]
        [Route("userapi/avatars/id/{avartarid}.json")]
        public IActionResult GetByID(uint avartarid)
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var Avatar = da.Avatars.Get(avartarid);
                if (Avatar == null)
                {
                    var JSONError = new JSONAvatarError();
                    JSONError.Error = "No avatar found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                var AvatarJSON = new JSONAvatar
                {
                    Avatar_ID = Avatar.avatar_id,
                    Shard_ID = Avatar.shard_id,
                    Name = Avatar.name,
                    Gender = Avatar.gender,
                    Date = Avatar.date,
                    Description = Avatar.description,
                    Current_Job = Avatar.current_job,
                    Mayor_Nhood = Avatar.mayor_nhood

                };

                return ApiResponse.Json(HttpStatusCode.OK, AvatarJSON);
            }
        }
        //get all online Avatars
        [HttpGet]
        [Route("userapi/avatars/online.json")]
        public IActionResult GetOnline()
        {
            var api = Api.INSTANCE;

            using (var da = api.DAFactory.Get())
            {
                var AvatarStatus = da.AvatarClaims.GetAll();
                if (AvatarStatus == null)
                {
                    var JSONError = new JSONAvatarError();
                    JSONError.Error = "No avatars found";
                    return ApiResponse.Json(HttpStatusCode.NotFound, JSONError);
                }

                List<JSONAvatarSmall> AvatarSmallJSON = new List<JSONAvatarSmall>();

                foreach (var Avatar in AvatarStatus)
                {
                    var online = da.Avatars.Get(Avatar.avatar_id);
                    uint Location = 0;
                    if (online.privacy_mode == 0)
                    {
                        Location = Avatar.location;
                    }
                    AvatarSmallJSON.Add(new JSONAvatarSmall
                    {
                        Avatar_ID = online.avatar_id,
                        Name = online.name,
                        Privacy_Mode = online.privacy_mode,
                        Location = Location
                    });

                }
                var AvatarJSON = new JSONAvatarOnline();
                AvatarJSON.Avatars_Online_Count = AvatarStatus.Count();
                AvatarJSON.Avatars = AvatarSmallJSON;
                return ApiResponse.Json(HttpStatusCode.OK, AvatarJSON);
            }
        }
    }
    public class JSONAvatarsPage
    {
        public int Page { get; set; }
        public int Total_Pages { get; set; }
        public int Total_Avatars { get; set; }
        public int Avatars_On_Page { get; set; }
        public List<JSONAvatar> Avatars { get; set; }
    }
    public class JSONAvatarError
    {
        public string Error { get; set; }
    }
    public class JSONAvatarOnline
    {
        public int Avatars_Online_Count { get; set; }
        public List<JSONAvatarSmall> Avatars { get; set; }
    }
    public class JSONAvatarSmall
    {
        public uint Avatar_ID { get; set; }
        public string Name { get; set; }
        public byte Privacy_Mode { get; set; }
        public uint Location { get; set; }
    }
    public class JSONAvatars
    {
        public List<JSONAvatar> Avatars { get; set; }
    }
    public class JSONAvatar
    {
        public uint Avatar_ID { get; set; }
        public int Shard_ID { get; set; }
        public string Name { get; set; }
        public DbAvatarGender Gender { get; set; }
        public uint Date { get; set; }
        public string Description { get; set; }
        public ushort Current_Job { get; set; }
        public int? Mayor_Nhood { get; set; }
    }
}
