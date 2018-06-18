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
using static FSO.Server.Api.Api;
using Ninject;
using FSO.Server.Utils;
using FSO.Server.Domain;

namespace FSO.Server.Servers.UserApi
{
    public class UserApi : AbstractServer
    {
        private IDisposable App;
        private ServerConfiguration Config;
        private IKernel Kernel;

        public event APIRequestShutdownDelegate OnRequestShutdown;
        public event APIBroadcastMessageDelegate OnBroadcastMessage;
        public event APIRequestUserDisconnectDelegate OnRequestUserDisconnect;
        public event APIRequestMailNotifyDelegate OnRequestMailNotify;

        public UserApi(ServerConfiguration config, IKernel kernel)
        {
            this.Config = config;
            this.Kernel = kernel;
        }

        public override void AttachDebugger(IServerDebugger debugger)
        {
        }

        public override void Shutdown()
        {
        }

        public override void Start()
        {
            // Start OWIN host 
            App = WebApp.Start(Config.Services.UserApi.Bindings[0], x =>
            {
                new UserApiStartup().Configuration(x, Config);
                var api = INSTANCE;
                api.OnBroadcastMessage += (s, t, m) => { OnBroadcastMessage?.Invoke(s, t, m); };
                api.OnRequestShutdown += (t, st) => { OnRequestShutdown?.Invoke(t, st); };
                api.OnRequestUserDisconnect += (i) => { OnRequestUserDisconnect?.Invoke(i); };
                api.OnRequestMailNotify += (i, s, b, t) => { OnRequestMailNotify?.Invoke(i, s, b, t); };
                api.HostPool = Kernel.Get<IGluonHostPool>();
            });
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
            settings.Add("cdnUrl", userApiConfig.CDNUrl);
            settings.Add("connectionString", config.Database.ConnectionString);
            settings.Add("NFSdir", config.SimNFS);
            settings.Add("smtpHost", userApiConfig.SmtpHost);
            settings.Add("smtpUser", userApiConfig.SmtpUser);
            settings.Add("smtpPassword", userApiConfig.SmtpPassword);
            settings.Add("smtpPort", userApiConfig.SmtpPort.ToString());
            settings.Add("useProxy", userApiConfig.UseProxy.ToString());

            var api = new FSO.Server.Api.Api();
            api.Init(settings);

            builder.UseWebApi(http);
        }
    }
}
