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


namespace FSO.Server.Servers.Api
{
    public class ApiServer : AbstractServer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private ApiServerConfiguration Config;
        private HttpListener Listener;
        private HttpRouter Router;
        private IKernel Kernel;

        private List<object> Controllers = new List<object>();

        public ApiServer(ApiServerConfiguration config, IKernel kernel)
        {
            this.Config = config;
            this.Kernel = kernel;
        }

        public override void Start()
        {
            LOG.Info("Starting API server");

            //TODO: .NET world is quite poor when it comes to embedded http servers. I don't know much about
            //the reliability or performance of HttpListener. Something to keep an eye on

            Listener = new HttpListener();
            foreach (var host in Config.Bindings)
            {
                Listener.Prefixes.Add(host);
            }
            Listener.Start();

            //Again, probably should not roll my own here, could not find anything satisfactory in open source world yet
            Router = new HttpRouter();

            foreach(var controller in Config.Controllers)
            {
                switch (controller)
                {
                    case ApiServerControllers.Auth:
                        AddController(typeof(AuthController));
                        break;
                }
            }

            var result = Listener.BeginGetContext(ListenerCallback, Listener);
        }

        private void AddController(Type controller)
        {
            Controllers.Add(Kernel.Get(controller, new ConstructorArgument("config", Config), new ConstructorArgument("router", Router)));
        }

        private void ListenerCallback(IAsyncResult result)
        {
            Listener.BeginGetContext(ListenerCallback, Listener);
            var context = Listener.EndGetContext(result);

            Router.Handle(context);
        }

        public override void Shutdown()
        {
            if (Listener != null)
            {
                Listener.Stop();
            }
        }
    }
}
