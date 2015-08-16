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
using FSO.Server.DataService.Shards;

namespace FSO.Server.Servers.Api.Controllers
{
    public class CitySelectorController : NancyModule
    {
        private static String ERROR_MISSING_TOKEN_CODE = "501";
        private static String ERROR_MISSING_TOKEN_MSG = "Token not found";

        private static String ERROR_EXPIRED_TOKEN_CODE = "502";
        private static String ERROR_EXPIRED_TOKEN_MSG = "Token has expired";

        public CitySelectorController(IDAFactory DAFactory, ApiServerConfiguration config, JWTFactory jwt, ShardsDataService shardsService) : base("/cityselector")
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

                var result = new XMLList<AvatarData>("The-Sims-Online");
                return Response.AsXml(result);
            };

            //Get a list of shards (cities)
            this.Get["/shard-status.jsp"] = _ =>
            {
                var result = new XMLList<ShardStatusItem>("Shard-Status-List");
                var shards = shardsService.GetShards();
                
                foreach(var shard in shards)
                {
                    var status = ShardStatus.Down;
                    switch (shard.status)
                    {
                        case Database.DA.Shards.ShardStatus.Up:
                            status = ShardStatus.Up;
                            break;
                        case Database.DA.Shards.ShardStatus.Full:
                            status = ShardStatus.Full;
                            break;
                        case Database.DA.Shards.ShardStatus.Frontier:
                            status = ShardStatus.Frontier;
                            break;
                        case Database.DA.Shards.ShardStatus.Down:
                            status = ShardStatus.Down;
                            break;
                        case Database.DA.Shards.ShardStatus.Closed:
                            status = ShardStatus.Closed;
                            break;
                        case Database.DA.Shards.ShardStatus.Busy:
                            status = ShardStatus.Busy;
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
