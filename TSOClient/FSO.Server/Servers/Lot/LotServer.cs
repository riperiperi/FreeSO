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
using FSO.Server.Database.DA.Hosts;
using FSO.Server.Servers.Shared.Handlers;

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
            Kernel.Bind<LotServer>().ToConstant(this);

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

                var oldAvatarClaims = db.AvatarClaims.GetAllByOwner(Config.Call_Sign).ToList();
                if (oldAvatarClaims.Count > 0)
                {
                    LOG.Warn("Detected " + oldAvatarClaims.Count + " avatar claims, perhaps the server did not shut down cleanly. Avatar consistency may be affected.");
                    db.AvatarClaims.DeleteAll(Config.Call_Sign);
                }
            }

            Connections = Kernel.Get<CityConnections>();
            Connections.OnCityDisconnected += Connections_OnCityDisconnected;
            Connections.Start();
        }

        private void Connections_OnCityDisconnected(CityConnection connection)
        {
            LOG.Warn("City connection panic, shutting down lots gracefully");
            Lots.ShutdownByShard(connection.CityConfig.ID);
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
                        da.Lots.DeleteLotServerTicket(packet.Password);
                    }


                    if (ticket != null)
                    {
                        uint location = 0;
                        if ((ticket.lot_id & 0x40000000) > 0) location = (uint)ticket.lot_id;
                        else location = da.Lots.Get(ticket.lot_id).location;

                        //We need to claim a lock for the avatar, if we can't do that we cant let them join
                        var didClaim = da.AvatarClaims.Claim(ticket.avatar_claim_id, ticket.avatar_claim_owner, Config.Call_Sign, location);
                        if (!didClaim)
                        {
                            rawSession.Close();
                            return;
                        }


                        //Time to upgrade to a voltron session
                        var newSession = Sessions.UpgradeSession<VoltronSession>(rawSession, x =>
                        {
                            x.UserId = ticket.user_id;
                            x.AvatarId = ticket.avatar_id;
                            x.IsAuthenticated = true;
                            x.AvatarClaimId = ticket.avatar_claim_id;
                        });

                        newSession.SetAttribute("cityCallSign", ticket.avatar_claim_owner);

                        //Try and join the lot, no reason to keep this connection alive if you can't get in
                        if (!Lots.TryJoin(ticket.lot_id, newSession))
                        {
                            newSession.Close();
                            using (var db = DAFactory.Get())
                            {
                                //return claim to the city we got it from.
                                db.AvatarClaims.Claim(newSession.AvatarClaimId, Config.Call_Sign, (string)newSession.GetAttribute("cityCallSign"), 0);
                            }
                        }
                        return;
                    }
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
                Lots.RouteMessage(session as IVoltronSession, message);
                return;
            }

            base.RouteMessage(session, message);
        }

        public override Type[] GetHandlers()
        {
            return new Type[] {
                typeof(CityServerAuthenticationHandler),
                typeof(LotNegotiationHandler),
                typeof(VoltronConnectionLifecycleHandler),
                typeof(ShardShutdownHandler),
                typeof(GluonAuthenticationHandler)
            };
        }

        public override void Shutdown()
        {
            var task = Lots.Shutdown();
            task.Wait();
            base.Shutdown();
        }

        protected override DbHost CreateHost()
        {
            var host = base.CreateHost();
            host.role = DbHostRole.lot;
            return host;
        }
    }
}
