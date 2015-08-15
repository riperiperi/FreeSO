using FSO.Server.Database.DA;
using FSO.Server.Servers;
using FSO.Server.Servers.Api;
using Ninject;
using Ninject.Parameters;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server
{
    public class ToolRunServer : ITool
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();

        private ServerConfiguration Config;
        private IKernel Kernel;

        public ToolRunServer(RunServerOptions options, ServerConfiguration config, IKernel kernel)
        {
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

            var daFactory = Kernel.Get<IDAFactory>();
            using (var da = daFactory.Get())
            {
                var admin = da.Users.GetByUsername("admin");
                int y = 22; 
            }




            List<AbstractServer> servers = new List<AbstractServer>();

            if(Config.Services.Api != null &&
                Config.Services.Api.Enabled)
            {
                servers.Add(
                    Kernel.Get<ApiServer>(new ConstructorArgument("config", Config.Services.Api))
                );
            }

            foreach(AbstractServer server in servers)
            {
                server.Start();
            }
        }
    }
}
