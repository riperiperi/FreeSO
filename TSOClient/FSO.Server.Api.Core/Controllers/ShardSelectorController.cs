using FSO.Server.Api.Core.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA.Shards;
using FSO.Server.Protocol.CitySelector;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace FSO.Server.Api.Core.Controllers
{
    [Route("cityselector/app/ShardSelectorServlet")]
    [ApiController]
    public class ShardSelectorController : ControllerBase
    {
        private static Func<IActionResult> ERROR_SHARD_NOT_FOUND = ApiResponse.XmlFuture(HttpStatusCode.OK, new XMLErrorMessage("503", "Shard not found"));
        private static Func<IActionResult> ERROR_AVATAR_NOT_FOUND = ApiResponse.XmlFuture(HttpStatusCode.OK, new XMLErrorMessage("504", "Avatar not found"));
        private static Func<IActionResult> ERROR_AVATAR_NOT_YOURS = ApiResponse.XmlFuture(HttpStatusCode.OK, new XMLErrorMessage("505", "You do not own this avatar!"));
        private static Func<IActionResult> ERROR_BANNED = ApiResponse.XmlFuture(HttpStatusCode.OK, new XMLErrorMessage("506", "Your account has been banned."));
        private static Func<IActionResult> ERROR_MAINTAINANCE = ApiResponse.XmlFuture(HttpStatusCode.OK, new XMLErrorMessage("507", "The server is currently undergoing maintainance. Please try again later."));

        public IActionResult Get(string shardName, string avatarId)
        {
            var api = Api.INSTANCE;

            var user = api.RequireAuthentication(Request);
            if (avatarId == null){
                //Using 0 to mean no avatar for CAS
                avatarId = "0";
            }

            using (var db = api.DAFactory.Get())
            {
                ShardStatusItem shard = api.Shards.GetByName(shardName);
                if (shard != null)
                {
                    var ip = ApiUtils.GetIP(Request);
                    uint avatarDBID = uint.Parse(avatarId);

                    if (avatarDBID != 0)
                    {
                        var avatar = db.Avatars.Get(avatarDBID);
                        if (avatar == null)
                        {
                            //can't join server with an avatar that doesn't exist
                            return ERROR_AVATAR_NOT_FOUND();
                        }
                        if (avatar.user_id != user.UserID || avatar.shard_id != shard.Id)
                        {
                            //make sure we own the avatar we're trying to connect with
                            return ERROR_AVATAR_NOT_YOURS();
                        }
                    }

                    var ban = db.Bans.GetByIP(ip);
                    var dbuser = db.Users.GetById(user.UserID);
                    if (dbuser == null || ban != null || dbuser.is_banned != false)
                    {
                        return ERROR_BANNED();
                    }

                    if (api.Config.Maintainance && !(dbuser.is_admin || dbuser.is_moderator))
                    {
                        return ERROR_MAINTAINANCE();
                    }

                    /** Make an auth ticket **/
                    var ticket = new ShardTicket
                    {
                        ticket_id = Guid.NewGuid().ToString().Replace("-", ""),
                        user_id = user.UserID,
                        avatar_id = avatarDBID,
                        date = Epoch.Now,
                        ip = ip
                    };

                    db.Users.UpdateConnectIP(ticket.user_id, ip);
                    db.Shards.CreateTicket(ticket);

                    var result = new ShardSelectorServletResponse();
                    result.PreAlpha = false;

                    result.Address = shard.PublicHost;
                    result.PlayerID = user.UserID;
                    result.AvatarID = avatarId;
                    result.Ticket = ticket.ticket_id;
                    result.ConnectionID = ticket.ticket_id;
                    result.AvatarID = avatarId;

                    return ApiResponse.Xml(HttpStatusCode.OK, result);
                }
                else
                {
                    return ERROR_SHARD_NOT_FOUND();
                }
            }
        }
    }
}