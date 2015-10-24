using FSO.Server.Database.DA;
using FSO.Server.DataService;
using FSO.Server.Debug;
using FSO.Server.Servers;
using FSO.Server.Servers.Api;
using FSO.Server.Servers.City;
using Ninject;
using Ninject.Extensions.ChildKernel;
using Ninject.Parameters;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FSO.Server
{
    public class ToolRunServer : ITool
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private ServerConfiguration Config;
        private IKernel Kernel;

        private bool Running;
        private List<AbstractServer> Servers;
        private RunServerOptions Options;

        public ToolRunServer(RunServerOptions options, ServerConfiguration config, IKernel kernel)
        {
            this.Options = options;
            this.Config = config;
            this.Kernel = kernel;
        }

        public void Run()
        {
            LOG.Info("Starting server");

            if(Config.Services == null)
            {
                LOG.Warn("No services found in the configuration file, exiting");
                return;
            }

            if (!Directory.Exists(Config.GameLocation))
            {
                LOG.Fatal("The directory specified as gameLocation in config.json does not exist");
                return;
            }

            //TODO: Some content preloading
            LOG.Info("Scanning content");
            Content.Content.Init(Config.GameLocation, Content.ContentMode.SERVER);
            Kernel.Bind<Content.Content>().ToConstant(Content.Content.Get());

            LOG.Info("Loading domain logic");
            Kernel.Load<Domain.DomainModule>();

            Servers = new List<AbstractServer>();

            if(Config.Services.Api != null &&
                Config.Services.Api.Enabled)
            {
                Servers.Add(
                    Kernel.Get<ApiServer>(new ConstructorArgument("config", Config.Services.Api))
                );
            }

            foreach(var cityServer in Config.Services.Cities){
                /**
                 * Need to create a kernel for each city server as there is some data they do not share
                 */
                var childKernel = new ChildKernel(
                    Kernel, 
                    new ShardDataServiceModule()
                );

                Servers.Add(
                    childKernel.Get<CityServer>(new ConstructorArgument("config", cityServer))
                );
            }

            Running = true;

            AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;

            NetworkDebugger debugInterface = null;

            if (Options.Debug)
            {
                debugInterface = new NetworkDebugger(Kernel);
                foreach (AbstractServer server in Servers)
                {
                    server.AttachDebugger(debugInterface);
                }
            }

            LOG.Info("Starting services");
            foreach (AbstractServer server in Servers)
            {
                server.Start();
            }

            //Hacky reference to maek sure the assembly is included
            FSO.Common.DatabaseService.Model.LoadAvatarByIDRequest x;

            if (debugInterface != null)
            {
                Application.EnableVisualStyles();
                Application.Run(debugInterface);
            }
            else
            {
                while (Running)
                {
                    Thread.Sleep(1000);
                }
            }
        }

        private void CurrentDomain_ProcessExit(object sender, EventArgs e)
        {
            foreach (AbstractServer server in Servers)
            {
                server.Shutdown();
            }

            Running = false;
        }
    }
}
