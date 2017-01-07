using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;

namespace FSO.Server.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            config.MapHttpAttributeRoutes();


            config.Routes.MapHttpRoute(
                name: "AuthLogin",
                routeTemplate: "AuthLogin",
                defaults: new {
                    controller = "AuthLogin"
                }
            );

            config.Routes.MapHttpRoute(
                name: "InitialConnectServlet",
                routeTemplate: "cityselector/app/InitialConnectServlet",
                defaults: new {
                    controller = "InitialConnect"
                }
            );

            config.Routes.MapHttpRoute(
                name: "AvatarDataServlet",
                routeTemplate: "cityselector/app/AvatarDataServlet",
                defaults: new {
                    controller = "AvatarData"
                }
            );
            
        }
    }
}
