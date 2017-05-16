using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Server.Common;
using Microsoft.Owin.Hosting;
using System.Net.Http;
using FSO.Server.Api;
using System.Web.Http;
using Owin;
using System.Collections.Specialized;
using FSO.Server.Servers.Api;

namespace FSO.Server.Servers.UserApi
{
    public class UserApi : AbstractServer
    {
        private IDisposable App;
        private ServerConfiguration Config;

        public UserApi(ServerConfiguration config)
        {
            this.Config = config;
        }

        public override void AttachDebugger(IServerDebugger debugger)
        {
        }

        public override void Shutdown()
        {
        }

        public override void Start()
        {
            string baseAddress = "http://localhost:9000/";

            // Start OWIN host 
            App = WebApp.Start(Config.Services.UserApi.Bindings[0], x =>
            {
                new UserApiStartup().Configuration(x, Config);
            });

            //Console.ReadLine();
        }
    }

    public class UserApiStartup
    {
        public void Configuration(IAppBuilder builder, ServerConfiguration config)
        {
            HttpConfiguration http = new HttpConfiguration();
            WebApiConfig.Register(http);

            var userApiConfig = config.Services.UserApi;

            var settings = new NameValueCollection();
            settings.Add("maintainance", userApiConfig.Maintainance.ToString());
            settings.Add("authTicketDuration", userApiConfig.AuthTicketDuration.ToString());
            settings.Add("regkey", userApiConfig.Regkey);
            settings.Add("secret", config.Secret);
            settings.Add("updateUrl", userApiConfig.UpdateUrl);
            settings.Add("connectionString", config.Database.ConnectionString);
            settings.Add("NFSdir", config.SimNFS);

            var api = new FSO.Server.Api.Api();
            api.Init(settings);

            builder.UseWebApi(http);
        }
    }
}
