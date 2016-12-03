using FSO.Common.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Servers.Api.JsonWebToken;
using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nancy.Security;
using FSO.Server.Database.DA.Shards;
using FSO.Common.Domain.Shards;
using NLog;

namespace FSO.Server.Servers.Api.Controllers
{
    public class CitySelectorController : NancyModule
    {
        private static String ERROR_MISSING_TOKEN_CODE = "501";
        private static String ERROR_MISSING_TOKEN_MSG = "Token not found";

        private static String ERROR_EXPIRED_TOKEN_CODE = "502";
        private static String ERROR_EXPIRED_TOKEN_MSG = "Token has expired";

        private static String ERROR_SHARD_NOT_FOUND_CODE = "503";
        private static String ERROR_SHARD_NOT_FOUND_MSG = "Shard not found";

        private static String ERROR_AVATAR_NOT_FOUND_CODE = "504";
        private static String ERROR_AVATAR_NOT_FOUND_MSG = "Avatar not found";

        private static String ERROR_AVATAR_NOT_YOURS_CODE = "505";
        private static String ERROR_AVATAR_NOT_YOURS_MSG = "You do not own this avatar!";

        private static Logger LOG = LogManager.GetCurrentClassLogger();

        public CitySelectorController(IDAFactory DAFactory, ApiServerConfiguration config, JWTFactory jwt, IShardsDomain shardsDomain) : base("/cityselector")
        {
            JsonWebToken.JWTTokenAuthentication.Enable(this, jwt);

            //Take the auth ticket, establish trust and then create a cookie (reusing JWT)
            this.Get["/app/InitialConnectServlet"] = _ =>
            {
                var ticketValue = this.Request.Query["ticket"];
                var version = this.Request.Query["version"];

                if (ticketValue == null)
                {
                    return Response.AsXml(new XMLErrorMessage(ERROR_MISSING_TOKEN_CODE, ERROR_MISSING_TOKEN_MSG));
                }
                
                using (var db = DAFactory.Get())
                {
                    var ticket = db.AuthTickets.Get((string)ticketValue);
                    if (ticket == null)
                    {
                        return Response.AsXml(new XMLErrorMessage(ERROR_MISSING_TOKEN_CODE, ERROR_MISSING_TOKEN_MSG));
                    }


                    db.AuthTickets.Delete((string)ticketValue);
                    if (ticket.date + config.AuthTicketDuration < Epoch.Now)
                    {
                        return Response.AsXml(new XMLErrorMessage(ERROR_EXPIRED_TOKEN_CODE, ERROR_EXPIRED_TOKEN_MSG));
                    }

                    /** Is it a valid account? **/
                    var user = db.Users.GetById(ticket.user_id);
                    if (user == null)
                    {
                        return Response.AsXml(new XMLErrorMessage(ERROR_MISSING_TOKEN_CODE, ERROR_MISSING_TOKEN_MSG));
                    }

                    //Use JWT to create and sign an auth cookies
                    var session = new JWTUserIdentity()
                    {
                        UserID = user.user_id,
                        UserName = user.username
                    };

                    var token = jwt.CreateToken(session);
                    return Response.AsXml(new UserAuthorized())
                            .WithCookie("fso", token.Token);
                }
            };

            //Return a list of the users avatars
            this.Get["/app/AvatarDataServlet"] = _ =>
            {
                this.RequiresAuthentication();
                var user = (JWTUserIdentity)this.Context.CurrentUser;

                var result = new XMLList<AvatarData>("The-Sims-Online");

                using (var db = DAFactory.Get())
                {
                    var avatars = db.Avatars.GetSummaryByUserId(user.UserID);

                    foreach(var avatar in avatars){
                        result.Add(new AvatarData {
                            ID = avatar.avatar_id,
                            Name = avatar.name,
                            ShardName = shardsDomain.GetById(avatar.shard_id).Name,
                            HeadOutfitID = avatar.head,
                            BodyOutfitID = avatar.body,
                            AppearanceType = (AvatarAppearanceType)Enum.Parse(typeof(AvatarAppearanceType), avatar.skin_tone.ToString()),
                            Description = avatar.description,
                            LotId = avatar.lot_id,
                            LotName = avatar.lot_name
                        });
                    }
                }

                return Response.AsXml(result);
            };

            this.Get["/app/ShardSelectorServlet"] = _ =>
            {
                this.RequiresAuthentication();
                var user = (JWTUserIdentity)this.Context.CurrentUser;

                var shardName = this.Request.Query["shardName"];
                var avatarId = this.Request.Query["avatarId"];
                if(avatarId  == null){
                    //Using 0 to mean no avatar for CAS
                    avatarId = "0";
                }

                using (var db = DAFactory.Get())
                {
                    ShardStatusItem shard = shardsDomain.GetByName(shardName);
                    if (shard != null)
                    {
                        var tryIP = Request.Headers["X-Forwarded-For"].FirstOrDefault();
                        var ip = tryIP ?? this.Request.UserHostAddress;

                        uint avatarDBID = uint.Parse(avatarId);

                        if (avatarDBID != 0)
                        {
                            var avatar = db.Avatars.Get(avatarDBID);
                            if (avatar == null) {
                                //can't join server with an avatar that doesn't exist
                                return Response.AsXml(new XMLErrorMessage(ERROR_AVATAR_NOT_FOUND_CODE, ERROR_AVATAR_NOT_FOUND_MSG));
                            }
                            if (avatar.user_id != user.UserID || avatar.shard_id != shard.Id)
                            {
                                //make sure we own the avatar we're trying to connect with
                                LOG.Info("SECURITY: Invalid avatar login attempt from " + ip + ", user "+user.UserID);
                                return Response.AsXml(new XMLErrorMessage(ERROR_AVATAR_NOT_YOURS_CODE, ERROR_AVATAR_NOT_YOURS_MSG));
                            }
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
                        db.Shards.CreateTicket(ticket);

                        var result = new ShardSelectorServletResponse();
                        result.PreAlpha = false;

                        result.Address = shard.PublicHost;
                        result.PlayerID = user.UserID;
                        result.Ticket = ticket.ticket_id;
                        result.ConnectionID = ticket.ticket_id;
                        result.AvatarID = avatarId;

                        return Response.AsXml(result);
                    }
                    else
                    {
                        return Response.AsXml(new XMLErrorMessage(ERROR_SHARD_NOT_FOUND_CODE, ERROR_SHARD_NOT_FOUND_MSG));
                    }
                }
            };

            //Get a list of shards (cities)
            this.Get["/shard-status.jsp"] = _ =>
            {
                var result = new XMLList<ShardStatusItem>("Shard-Status-List");
                var shards = shardsDomain.All;
                
                foreach(var shard in shards)
                {
                    var status = Protocol.CitySelector.ShardStatus.Down;
                    /*switch (shard.Status)
                    {
                        case Database.DA.Shards.ShardStatus.Up:
                            status = Protocol.CitySelector.ShardStatus.Up;
                            break;
                        case Database.DA.Shards.ShardStatus.Full:
                            status = Protocol.CitySelector.ShardStatus.Full;
                            break;
                        case Database.DA.Shards.ShardStatus.Frontier:
                            status = Protocol.CitySelector.ShardStatus.Frontier;
                            break;
                        case Database.DA.Shards.ShardStatus.Down:
                            status = Protocol.CitySelector.ShardStatus.Down;
                            break;
                        case Database.DA.Shards.ShardStatus.Closed:
                            status = Protocol.CitySelector.ShardStatus.Closed;
                            break;
                        case Database.DA.Shards.ShardStatus.Busy:
                            status = Protocol.CitySelector.ShardStatus.Busy;
                            break;
                    }*/

                    result.Add(shard);
                }

                return Response.AsXml(result);
            };
        }
    }
    
}
