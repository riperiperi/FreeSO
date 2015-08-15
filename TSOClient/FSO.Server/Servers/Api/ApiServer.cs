using FSO.Server.Framework;
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

namespace FSO.Server.Servers.Api
{
    public class ApiServer : AbstractServer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private ApiServerConfiguration Config;
        private IKernel Kernel;
        private NancyHost Nancy;

        public ApiServer(ApiServerConfiguration config, IKernel kernel)
        {
            this.Config = config;
            this.Kernel = kernel;
        }

        public override void Start()
        {
            LOG.Info("Starting API server");


            var configuration = new HostConfiguration();
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
    }


    class CustomNancyBootstrap : NinjectNancyBootstrapper
    {
        private IKernel Kernel;

        public CustomNancyBootstrap(IKernel kernel)
        {
            this.Kernel = kernel;
        }

        protected override IKernel GetApplicationContainer()
        {
            return this.Kernel;
        }
    }
}
