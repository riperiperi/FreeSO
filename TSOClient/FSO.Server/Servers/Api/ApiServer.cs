using FSO.Server.Servers.Api.Controllers;
using Ninject;
using Ninject.Parameters;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using Nancy.Hosting.Self;
using Nancy.Bootstrappers.Ninject;
using Nancy.Bootstrapper;
using Nancy;
using FSO.Server.Common;
using FSO.Server.Protocol.Gluon.Model;

namespace FSO.Server.Servers.Api
{
    public class ApiServer : AbstractServer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private ApiServerConfiguration Config;
        private IKernel Kernel;
        private NancyHost Nancy;

        //TODO: connect to shards to do these? right now this assumes the API server is on the same server as all shards.
        //would mean we could move these out of this class too.
        public event APIRequestShutdownDelegate OnRequestShutdown;
        public event APIBroadcastMessageDelegate OnBroadcastMessage;

        public delegate void APIRequestShutdownDelegate(uint time, ShutdownType type);
        public delegate void APIBroadcastMessageDelegate(string sender, string title, string message);

        public ApiServer(ApiServerConfiguration config, IKernel kernel)
        {
            this.Config = config;
            this.Kernel = kernel;

            Kernel.Bind<ApiServer>().ToConstant(this);
            Kernel.Bind<ApiServerConfiguration>().ToConstant(config);
        }

        public override void Start()
        {
            LOG.Info("Starting API server");

            var configuration = new HostConfiguration();
            configuration.UrlReservations.CreateAutomatically = true;
            var uris = new List<Uri>();

            foreach(var path in Config.Bindings)
            {
                uris.Add(new Uri(path));
            }

            Nancy = new NancyHost(new CustomNancyBootstrap(Kernel), configuration, uris.ToArray());
            Nancy.Start();
        }

        public override void Shutdown()
        {
            if(Nancy != null)
            {
                Nancy.Stop();
            }
        }

        public void RequestShutdown(uint time, ShutdownType type)
        {
            OnRequestShutdown?.Invoke(time, type);
        }

        public void BroadcastMessage(string sender, string title, string message)
        {
            OnBroadcastMessage?.Invoke(sender, title, message);
        }

        public override void AttachDebugger(IServerDebugger debugger)
        {
        }
    }


    class CustomNancyBootstrap : NinjectNancyBootstrapper
    {
        private IKernel Kernel;

        public CustomNancyBootstrap(IKernel kernel)
        {
            this.Kernel = kernel;
        }

        protected override void ApplicationStartup(IKernel container, IPipelines pipelines)
        {
            base.ApplicationStartup(container, pipelines);

            pipelines.AfterRequest.AddItemToEndOfPipeline(x =>
                x.Response.WithHeader("Access-Control-Allow-Origin", "*")
                          .WithHeader("Access-Control-Allow-Methods", "DELETE, GET, HEAD, POST, PUT, OPTIONS, PATCH")
                          .WithHeader("Access-Control-Allow-Headers", "Content-Type, Authorization")
                          .WithHeader("Access-Control-Expose-Headers", "X-Total-Count")
            );
        }

        protected override IKernel GetApplicationContainer()
        {
            return this.Kernel;
        }
    }
}
