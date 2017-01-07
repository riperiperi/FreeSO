using FSO.Server.Database.DA;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Configuration;
using System.Web.Http;
using System.Web.Routing;

namespace FSO.Server.Api
{
    public class WebApiApplication : System.Web.HttpApplication
    {
        public static IDAFactory DAFactory;

        protected void Application_Start()
        {
            GlobalConfiguration.Configure(WebApiConfig.Register);
            new Api().Init();
        }
    }
}
