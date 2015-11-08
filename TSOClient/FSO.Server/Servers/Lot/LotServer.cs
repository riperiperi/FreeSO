using FSO.Server.Framework.Aries;
using FSO.Server.Servers.Lot.Handlers;
using FSO.Server.Servers.Lot.Lifecycle;
using Ninject;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot
{
    public class LotServer : AbstractAriesServer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private LotServerConfiguration Config;
        private CityConnections Connections;

        public LotServer(LotServerConfiguration config, Ninject.IKernel kernel) : base(config, kernel)
        {
            this.Config = config;
        }

        public override void Start()
        {
            LOG.Info("Starting lot hosting server");
            base.Start();
        }

        protected override void Bootstrap()
        {
            base.Bootstrap();

            Kernel.Bind<LotServerConfiguration>().ToConstant(Config);

            Connections = Kernel.Get<CityConnections>();
            Kernel.Bind<CityConnections>().ToConstant(Connections);
            Connections.Start();


            /**
             * Tasks:
             *  2) Advertise avaliability
             *  3) Negotiate a lot going online
             *  4) Communicate lot status. Visitor count, who is where etc
             */
        }

        public override Type[] GetHandlers()
        {
            return new Type[] {
                typeof(CityServerAuthenticationHandler)
            };
        }
    }
}
