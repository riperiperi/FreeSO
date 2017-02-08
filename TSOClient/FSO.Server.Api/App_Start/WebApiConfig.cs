using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Cors;

namespace FSO.Server.Api
{
    public static class WebApiConfig
    {
        public static void Register(HttpConfiguration config)
        {
            // Web API configuration and services

            // Web API routes
            var cors = new EnableCorsAttribute("*", "*", "*");
            config.EnableCors(cors);
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

            config.Routes.MapHttpRoute(
                name: "ShardStatus",
                routeTemplate: "cityselector/shard-status.jsp",
                defaults: new
                {
                    controller = "ShardStatus"
                },
                constraints: null
            );

            config.Routes.MapHttpRoute(
                name: "ShardSelectorServlet",
                routeTemplate: "cityselector/app/ShardSelectorServlet",
                defaults: new
                {
                    controller = "ShardSelector"
                }
            );

            config.Routes.MapHttpRoute(
                name: "Registration",
                routeTemplate: "userapi/registration",
                defaults: new
                {
                    controller = "Registration"
                }
            );
        }
    }
}
