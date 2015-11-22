using FSO.Server.Database.DA;
using FSO.Server.Framework.Aries;
using FSO.Server.Servers.Lot.Domain;
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

            Kernel.Bind<LotServerConfiguration>().ToConstant(Config);
            Kernel.Bind<LotHost>().To<LotHost>().InSingletonScope();
            Kernel.Bind<CityConnections>().To<CityConnections>().InSingletonScope();
        }

        public override void Start()
        {
            LOG.Info("Starting lot hosting server");
            base.Start();
        }

        protected override void Bootstrap()
        {
            base.Bootstrap();

            IDAFactory da = Kernel.Get<IDAFactory>();
            using (var db = da.Get())
            {
                var oldClaims = db.LotClaims.GetAllByOwner(Config.Call_Sign).ToList();
                if (oldClaims.Count > 0)
                {
                    LOG.Warn("Detected " + oldClaims.Count + " previously allocated lot claims, perhaps the server did not shut down cleanly. Lot consistency may be affected.");
                    db.LotClaims.RemoveAllByOwner(Config.Call_Sign);
                }
            }

            Connections = Kernel.Get<CityConnections>();
            Connections.Start();
        }

        public override Type[] GetHandlers()
        {
            return new Type[] {
                typeof(CityServerAuthenticationHandler),
                typeof(LotNegotiationHandler)
            };
        }
    }
}
