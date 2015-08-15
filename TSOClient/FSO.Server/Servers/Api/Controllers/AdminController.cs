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

            this.Get["/users/current"] = _ =>
            {
                this.RequiresAuthentication();
                return null;
            };
        }
    }


}
