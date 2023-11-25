using FSO.Common.Utils;
using FSO.Server.Api.Core.Utils;
using FSO.Server.Protocol.CitySelector;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace FSO.Server.Api.Core.Controllers
{
    [EnableCors]
    [Route("cityselector/app/AvatarDataServlet")]
    [ApiController]
    public class AvatarDataController : ControllerBase
    {
        public IActionResult Get()
        {
            var api = Api.INSTANCE;
            var user = api.RequireAuthentication(Request);
            
            var result = new XMLList<AvatarData>("The-Sims-Online");

            using (var db = api.DAFactory.Get())
            {
                var avatars = db.Avatars.GetSummaryByUserId(user.UserID);

                foreach (var avatar in avatars)
                {
                    result.Add(new AvatarData
                    {
                        ID = avatar.avatar_id,
                        Name = avatar.name,
                        ShardName = api.Shards.GetById(avatar.shard_id).Name,
                        HeadOutfitID = avatar.head,
                        BodyOutfitID = avatar.body,
                        AppearanceType = (AvatarAppearanceType)Enum.Parse(typeof(AvatarAppearanceType), avatar.skin_tone.ToString()),
                        Description = avatar.description,
                        LotId = avatar.lot_id,
                        LotName = avatar.lot_name,
                        LotLocation = avatar.lot_location
                    });
                }
            }

            return ApiResponse.Xml(HttpStatusCode.OK, result);
        }
    }
}