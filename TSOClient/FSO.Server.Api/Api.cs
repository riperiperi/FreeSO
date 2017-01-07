using FSO.Server.Database.DA;
using FSO.Server.Domain;
using FSO.Server.Servers.Api.JsonWebToken;
using System;
using System.Collections.Generic;
using System.Linq;
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

        public void Init()
        {
            Config = new ApiConfig();
            Config.Maintainance = bool.Parse(WebConfigurationManager.AppSettings["maintainance"]);
            Config.AuthTicketDuration = int.Parse(WebConfigurationManager.AppSettings["authTicketDuration"]);
            Config.Regkey = WebConfigurationManager.AppSettings["regkey"];
            Config.Secret = WebConfigurationManager.AppSettings["secret"];
            Config.UpdateUrl = WebConfigurationManager.AppSettings["updateUrl"];

            JWT = new JWTFactory(new JWTConfiguration()
            {
                Key = System.Text.UTF8Encoding.UTF8.GetBytes(Config.Secret)
            });

            DAFactory = new MySqlDAFactory(new Database.DatabaseConfiguration()
            {
                ConnectionString = WebConfigurationManager.AppSettings["connectionString"]
            });

            Shards = new Shards(DAFactory);
            Shards.AutoUpdate();
        }
    }
}