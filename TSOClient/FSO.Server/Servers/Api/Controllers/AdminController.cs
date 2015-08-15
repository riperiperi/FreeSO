using FSO.Server.Database.DA;
using FSO.Server.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Api.Controllers
{
    /// <summary>
    /// Provides administration APIs for server setup
    /// </summary>
    public class AdminController
    {
        private IDAFactory DAFactory;

        public AdminController(ApiServerConfiguration config, HttpRouter router, IDAFactory daFactory)
        {
            this.DAFactory = daFactory;

            //router.Post("/oauth/token", new HttpHandler(OAuthToken));
        }

        private void OAuthToken(HttpListenerRequest request, HttpListenerResponse response)
        {
            
        }
    }
}
