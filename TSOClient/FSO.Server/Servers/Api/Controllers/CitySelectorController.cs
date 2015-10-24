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
using FSO.Server.Domain;

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

        public CitySelectorController(IDAFactory DAFactory, ApiServerConfiguration config, JWTFactory jwt, IDomain domain) : base("/cityselector")
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
                    var avatars = db.Avatars.GetByUserId(user.UserID);

                    foreach(var avatar in avatars){
                        result.Add(new AvatarData {
                            ID = avatar.avatar_id,
                            Name = avatar.name,
                            ShardName = domain.Shards.GetById(avatar.shard_id).name,
                            HeadOutfitID = avatar.head,
                            BodyOutfitID = avatar.body,
                            AppearanceType = (AvatarAppearanceType)Enum.Parse(typeof(AvatarAppearanceType), avatar.skin_tone.ToString()),
                            Description = avatar.description
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
                    var shard = domain.Shards.GetByName(shardName);
                    if (shard != null)
                    {
                        var avatarDBID = uint.Parse(avatarId);

                        /** Make an auth ticket **/
                        var ticket = new ShardTicket
                        {
                            ticket_id = Guid.NewGuid().ToString().Replace("-", ""),
                            user_id = user.UserID,
                            avatar_id = avatarDBID,
                            date = Epoch.Now,
                            ip = this.Request.UserHostAddress
                        };
                        db.Shards.CreateTicket(ticket);

                        var result = new ShardSelectorServletResponse();
                        result.PreAlpha = false;

                        result.Address = shard.public_host;
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
                var shards = domain.Shards.All;
                
                foreach(var shard in shards)
                {
                    var status = Protocol.CitySelector.ShardStatus.Down;
                    switch (shard.status)
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
                    }

                    result.Add(new ShardStatusItem()
                    {
                        Name = shard.name,
                        Rank = shard.rank,
                        Map = shard.map
                    });
                }

                return Response.AsXml(result);
            };
        }
    }
    
}
