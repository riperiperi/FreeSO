using FSO.Server.Api.Core.Utils;
using FSO.Server.Common;
using FSO.Server.Protocol.CitySelector;
using FSO.Server.Servers.Api.JsonWebToken;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;

namespace FSO.Server.Api.Core.Controllers
{
    [Route("cityselector/app/InitialConnectServlet")]
    [ApiController]
    public class InitialConnectController : ControllerBase
    {
        private static Func<IActionResult> ERROR_MISSING_TOKEN = ApiResponse.XmlFuture(HttpStatusCode.OK, new XMLErrorMessage("501", "Token not found"));
        private static Func<IActionResult> ERROR_EXPIRED_TOKEN = ApiResponse.XmlFuture(HttpStatusCode.OK, new XMLErrorMessage("502", "Token has expired"));

        [HttpGet]
        public IActionResult Get(string ticket, string version)
        {
            var api = Api.INSTANCE;

            if (ticket == null || ticket == "" || version == null){
                return ERROR_MISSING_TOKEN();
            }

            using (var db = api.DAFactory.Get())
            {
                var dbTicket = db.AuthTickets.Get(ticket);
                if (dbTicket == null){
                    return ERROR_MISSING_TOKEN();
                }

                db.AuthTickets.Delete((string)ticket);
                if (dbTicket.date + api.Config.AuthTicketDuration < Epoch.Now){
                    return ERROR_EXPIRED_TOKEN();
                }

                /** Is it a valid account? **/
                var user = db.Users.GetById(dbTicket.user_id);
                if (user == null){
                    return ERROR_MISSING_TOKEN();
                }

                //Use JWT to create and sign an auth cookies
                var session = new JWTUser()
                {
                    UserID = user.user_id,
                    UserName = user.username
                };

                //TODO: This assumes 1 shard, when using multiple need to either have version download occour after
                //avatar select, or rework the tables
                var shardOne = api.Shards.GetById(1);

                var token = api.JWT.CreateToken(session);

                IActionResult response;
                if (shardOne.UpdateID != null)
                {
                    var update = db.Updates.GetUpdate(shardOne.UpdateID.Value);
                    response = ApiResponse.Xml(HttpStatusCode.OK, new UserAuthorized()
                    {
                        FSOBranch = shardOne.VersionName,
                        FSOVersion = shardOne.VersionNumber,
                        FSOUpdateUrl = update.full_zip,
                        FSOCDNUrl = api.Config.CDNUrl
                    });
                }
                else
                {
                    response = ApiResponse.Xml(HttpStatusCode.OK, new UserAuthorized()
                    {
                        FSOBranch = shardOne.VersionName,
                        FSOVersion = shardOne.VersionNumber,
                        FSOUpdateUrl = api.Config.UpdateUrl,
                        FSOCDNUrl = api.Config.CDNUrl
                    });
                }
                Response.Cookies.Append("fso", token.Token, new Microsoft.AspNetCore.Http.CookieOptions()
                {
                    Expires = DateTimeOffset.Now.AddDays(1),
                    Domain = Request.Host.Host,
                    Path = "/"
                });
                //HttpContext.Current.Response.SetCookie(new HttpCookie("fso", token.Token));
                return response;
            }
        }

    }
}
 