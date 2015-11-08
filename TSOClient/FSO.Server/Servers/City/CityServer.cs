using FSO.Common.Domain.Shards;
using FSO.Server.Framework;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.Server.Servers.City.Handlers;
using Ninject;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City
{
    public class CityServer : AbstractAriesServer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private CityServerConfiguration Config;
        private ISessionGroup VoltronSessions;

        public CityServer(CityServerConfiguration config, IKernel kernel) : base(config, kernel)
        {
            this.Config = config;
            VoltronSessions = Sessions.GetOrCreateGroup(Groups.VOLTRON);
        }

        public override void Start()
        {
            LOG.Info("Starting city server for city: " + Config.ID);
            base.Start();
        }

        protected override void Bootstrap()
        {
            var shards = Kernel.Get<IShardsDomain>();
            var shard = shards.GetById(Config.ID);
            if (shard == null)
            {
                throw new Exception("Unable to find a shard with id " + Config.ID + ", check it exists in the database");
            }

            LOG.Info("City identified as " + shard.Name);

            var context = new CityServerContext();
            context.ShardId = shard.Id;
            context.Config = Config;
            Kernel.Bind<CityServerContext>().ToConstant(context);
            Kernel.Bind<int>().ToConstant(shard.Id).Named("ShardId");
            Kernel.Bind<CityServerConfiguration>().ToConstant(Config);

            base.Bootstrap();
        }

        public override Type[] GetHandlers()
        {
            return new Type[]{
                typeof(SetPreferencesHandler),
                typeof(RegistrationHandler),
                typeof(DataServiceWrapperHandler),
                typeof(DBRequestWrapperHandler),
                typeof(VoltronConnectionLifecycleHandler),
                typeof(FindPlayerHandler),
                typeof(PurchaseLotHandler),
                typeof(LotServerAuthenticationHandler),
                typeof(MessagingHandler)
            };
        }
    }
}
