using System;
using FSO.Server.Common;
using Microsoft.Owin.Hosting;
using System.Web.Http;
using Owin;
using System.Collections.Specialized;
using Ninject;
using FSO.Server.Domain;

using static FSO.Server.Common.ApiAbstract;
using NLog;

namespace FSO.Server.Servers.UserApi
{
    public class UserApi : AbstractServer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        public ServerConfiguration Config;
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
            APIThread?.Stop();
        }

        private IAPILifetime APIThread;

        public static Func<UserApi, string, IAPILifetime> CustomStartup;
        public override void Start()
        {
            // Start OWIN host
            if (CustomStartup != null) APIThread = CustomStartup(this, Config.Services.UserApi.Bindings[0]);
            else
            {
                LOG.Error("No startup function injected for UserApi, server not started!");
            }
        }

        public IGluonHostPool GetGluonHostPool()
        {
            return Kernel.Get<IGluonHostPool>();
        }

        public void SetupInstance(ApiAbstract api)
        {
            api.OnBroadcastMessage += (s, t, m) => { OnBroadcastMessage?.Invoke(s, t, m); };
            api.OnRequestShutdown += (t, st) => { OnRequestShutdown?.Invoke(t, st); };
            api.OnRequestUserDisconnect += (i) => { OnRequestUserDisconnect?.Invoke(i); };
            api.OnRequestMailNotify += (i, s, b, t) => { OnRequestMailNotify?.Invoke(i, s, b, t); };
            
            var config = Config;
        }
    }
}
