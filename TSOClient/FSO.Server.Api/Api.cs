using FSO.Server.Database.DA;
using FSO.Server.Domain;
using FSO.Server.Servers.Api.JsonWebToken;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Http;
using System.Security;
using System.Threading.Tasks;
using System.Web;
using System.Web.Configuration;

namespace FSO.Server.Api
{
    public class Api
    {
        public static Api INSTANCE;

        public IDAFactory DAFactory;
        public ApiConfig Config;
        public JWTFactory JWT;
        public Shards Shards;

        public Api()
        {
            INSTANCE = this;
        }

        public void Init(NameValueCollection appSettings)
        {
            Config = new ApiConfig();
            Config.Maintainance = bool.Parse(appSettings["maintainance"]);
            Config.AuthTicketDuration = int.Parse(appSettings["authTicketDuration"]);
            Config.Regkey = appSettings["regkey"];
            Config.Secret = appSettings["secret"];
            Config.UpdateUrl = appSettings["updateUrl"];

            JWT = new JWTFactory(new JWTConfiguration()
            {
                Key = System.Text.UTF8Encoding.UTF8.GetBytes(Config.Secret)
            });

            DAFactory = new MySqlDAFactory(new Database.DatabaseConfiguration()
            {
                ConnectionString = appSettings["connectionString"]
            });

            Shards = new Shards(DAFactory);
            Shards.AutoUpdate();
        }

        public JWTUser RequireAuthentication(HttpRequestMessage request)
        {
            /*var http = HttpContext.Current;
            if (http == null)
            {
                throw new SecurityException("Unable to get http context");
            }*/

            var cookies = request.Headers.GetCookies().FirstOrDefault();
            if (cookies == null)
                throw new SecurityException("Unable to find cookie");


            var cookie = cookies["fso"];
            if (cookie == null)
            {
                throw new SecurityException("Unable to find cookie");
            }

            var result = JWT.DecodeToken(cookie.Value);
            if(result == null)
            {
                throw new SecurityException("Invalid token");
            }

            return result;
        }
    }
}