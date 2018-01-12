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
            cors.SupportsCredentials = true;
            config.EnableCors(cors);
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(
                name: "AuthLogin",
                routeTemplate: "AuthLogin",
                defaults: new
                {
                    controller = "AuthLogin"
                }
            );

            config.Routes.MapHttpRoute(
                name: "InitialConnectServlet",
                routeTemplate: "cityselector/app/InitialConnectServlet",
                defaults: new
                {
                    controller = "InitialConnect"
                }
            );

            config.Routes.MapHttpRoute(
                name: "AvatarDataServlet",
                routeTemplate: "cityselector/app/AvatarDataServlet",
                defaults: new
                {
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

            config.Routes.MapHttpRoute(
                name: "LotThumb",
                routeTemplate: "userapi/city/{shardid}/{id}.png",
                defaults: new
                {
                    controller = "LotThumb"
                });

            config.Routes.MapHttpRoute(
                name: "CityJSON",
                routeTemplate: "userapi/city/{shardid}/city.json",
                defaults: new
                {
                    controller = "CityJSON"
                });

            //ADMIN API
            config.Routes.MapHttpRoute(
                name: "AdminOAuth",
                routeTemplate: "admin/oauth/token",
                defaults: new
                {
                    controller = "AdminOAuth"
                });

            config.Routes.MapHttpRoute(
                name: "AdminUsers",
                routeTemplate: "admin/users/{id}",
                defaults: new
                {
                    controller = "AdminUsers"
                });
            config.Routes.MapHttpRoute(
                name: "AdminUsers2",
                routeTemplate: "admin/users",
                defaults: new
                {
                    controller = "AdminUsers"
                });

            config.Routes.MapHttpRoute(
                name: "AdminUsers3",
                routeTemplate: "admin/ban",
                defaults: new
                {
                    controller = "AdminUsers"
                }
                );

            config.Routes.MapHttpRoute(
                name: "AdminShards",
                routeTemplate: "admin/shards",
                defaults: new
                {
                    controller = "AdminShards"
                });

            config.Routes.MapHttpRoute(
                name: "AdminShards2",
                routeTemplate: "admin/shards/{action}",
                defaults: new
                {
                    controller = "AdminShards"
                });

            config.Routes.MapHttpRoute(
                name: "AdminTasks",
                routeTemplate: "admin/tasks",
                defaults: new
                {
                    controller = "AdminTasks"
                });

            config.Routes.MapHttpRoute(
                name: "AdminTasks2",
                routeTemplate: "admin/tasks/{action}",
                defaults: new
                {
                    controller = "AdminTasks",
                });

            config.Routes.MapHttpRoute(
                name: "AdminHosts",
                routeTemplate: "admin/hosts",
                defaults: new
                {
                    controller = "AdminHosts"
                });
        }
    }
}
