using FSO.Server.Database.DA;
using Nancy;
using Nancy.Authentication.Stateless;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Nancy.Authentication.Token;
using Nancy.Security;
using FSO.Server.Common;
using FSO.Server.Servers.Api.JsonWebToken;
using FSO.Server.Database.DA.Users;

namespace FSO.Server.Servers.Api.Controllers
{
    /// <summary>
    /// Provides administration APIs for server setup
    /// </summary>
    public class AdminController : NancyModule
    {
        private IDAFactory DAFactory;

        public AdminController(IDAFactory daFactory, JWTConfiguration jwtConfig) : base("/admin")
        {
            JWTTokenAuthentication.Enable(this, jwtConfig);

            this.DAFactory = daFactory;

            this.After.AddItemToEndOfPipeline(x =>
            {
                x.Response.WithHeader("Access-Control-Allow-Origin", "*");
            });

            this.Get["/users/current"] = _ =>
            {
                this.RequiresAuthentication();
                JWTUserIdentity user = (JWTUserIdentity)this.Context.CurrentUser;

                using (var da = daFactory.Get())
                {
                    var userModel = da.Users.GetById(user.UserID);
                    if (userModel == null) { throw new Exception("Unable to find user"); }
                    return Response.AsJson<User>(userModel);
                }
            };

            this.Get["/users/{id}"] = parameters =>
            {
                this.DemandModerator();

                using (var da = daFactory.Get())
                {
                    var userModel = da.Users.GetById((uint)parameters.id);
                    if (userModel == null) { throw new Exception("Unable to find user"); }
                    return Response.AsJson<User>(userModel);
                }
            };

            this.Get["/users"] = _ =>
            {
                this.DemandModerator();
                return null;
            };
        }
    }





    static class AuthExtensions
    {
        public static void DemandModerator(this AdminController controller)
        {
            controller.RequiresAuthentication();
            controller.RequiresClaims(new string[] { "moderator" });
        }

        public static void DemandAdmin(this AdminController controller)
        {
            controller.RequiresAuthentication();
            controller.RequiresClaims(new string[] { "admin" });
        }
    }

}
