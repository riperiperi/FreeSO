using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Aries.Packets;
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

        private LotHost Lots;

        public LotServer(LotServerConfiguration config, Ninject.IKernel kernel) : base(config, kernel)
        {
            this.Config = config;

            Kernel.Bind<LotServerConfiguration>().ToConstant(Config);
            Kernel.Bind<LotHost>().To<LotHost>().InSingletonScope();
            Kernel.Bind<CityConnections>().To<CityConnections>().InSingletonScope();

            Lots = Kernel.Get<LotHost>();
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

        protected override void HandleVoltronSessionResponse(IAriesSession session, object message)
        {
            var rawSession = (AriesSession)session;
            var packet = message as RequestClientSessionResponse;

            if (message != null)
            {
                DbLotServerTicket ticket = null;

                using (var da = DAFactory.Get())
                {
                    ticket = da.Lots.GetLotServerTicket(packet.Password);
                    if (ticket != null)
                    {
                        //TODO: Check if its expired
                        da.Shards.DeleteTicket(packet.Password);
                    }
                }

                if(ticket != null)
                {
                    //Time to upgrade to a voltron session
                    var newSession = Sessions.UpgradeSession<VoltronSession>(rawSession, x => {
                        x.UserId = ticket.user_id;
                        x.AvatarId = ticket.avatar_id;
                        x.IsAuthenticated = true;
                    });

                    //We need to claim a lock for the avatar, if we can't do that we cant let them join


                    //Try and join the lot, no reason to keep this connection alive if you can't get in
                    if (!Lots.TryJoin(ticket.lot_id, newSession))
                    {
                        newSession.Close();
                    }
                    return;
                }
            }

            //Failed authentication
            rawSession.Close();
        }

        protected override void RouteMessage(IAriesSession session, object message)
        {
            if(session is IVoltronSession)
            {
                //Route to a specific lot
                return;
            }

            base.RouteMessage(session, message);
        }

        public override Type[] GetHandlers()
        {
            return new Type[] {
                typeof(CityServerAuthenticationHandler),
                typeof(LotNegotiationHandler),
                typeof(VoltronConnectionLifecycleHandler)
            };
        }
    }
}
