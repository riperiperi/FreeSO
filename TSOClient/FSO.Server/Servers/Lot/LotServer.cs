using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Hosts;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Domain;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.Server.Servers.Lot.Domain;
using FSO.Server.Servers.Lot.Handlers;
using FSO.Server.Servers.Lot.Lifecycle;
using FSO.Server.Servers.Lot.Surround;
using FSO.Server.Servers.Shared.Handlers;
using Ninject;
using NLog;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot
{
    public class LotServer : AbstractAriesServer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private LotServerConfiguration Config;
        private CityConnections Connections;
        private System.Timers.Timer LotLivenessTimer = new System.Timers.Timer(60000);

        private LotHost Lots;

        public LotServer(LotServerConfiguration config, Ninject.IKernel kernel) : base(config, kernel)
        {
            this.Config = config;
            this.UnexpectedDisconnectWaitSeconds = 30;
            this.TimeoutIfNoAuth = config.Timeout_No_Auth;

            Kernel.Bind<LotServerConfiguration>().ToConstant(Config);
            Kernel.Bind<LotHost>().To<LotHost>().InSingletonScope();
            Kernel.Bind<CityConnections>().To<CityConnections>().InSingletonScope();
            Kernel.Bind<LiveSurroundHost>().To<LiveSurroundHost>().InSingletonScope();
            Kernel.Bind<LotServer>().ToConstant(this);

            LotLivenessTimer.AutoReset = true;
            LotLivenessTimer.Elapsed += LivenessCheck;

            Lots = Kernel.Get<LotHost>();
        }

        private void LivenessCheck(object sender, System.Timers.ElapsedEventArgs e)
        {
            Lots.CheckLiveness();
        }

        public override void Start()
        {
            LOG.Info("Starting lot hosting server");
            LotLivenessTimer.Start();
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

        private async void Connections_OnCityDisconnected(CityConnection connection)
        {
            LOG.Warn("City connection lost... if it's not back in 30 seconds all its lots will be closed!");
            await Task.Delay(30000);
            try
            {
                if (!connection.Connected)
                    Lots.ShutdownByShard(connection.CityConfig.ID);
            } catch
            {

            }
        }

        protected override void HandleVoltronSessionResponse(IAriesSession session, object message)
        {
            var rawSession = (AriesSession)session;
            var packet = message as RequestClientSessionResponse;

            if (message != null)
            {
                if (packet.Unknown2 == 1)
                {
                    //connection re-establish.
                    if (!AttemptMigration(rawSession, packet.User, packet.Password))
                    {
                        //failed to find a session to migrate
                        rawSession.Write(new ServerByePDU() { }); //try and close the connection safely
                        rawSession.Close();
                    }
                    return;
                }

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
                        if ((ticket.lot_id & (uint)LotIdFlags.SpecialMask) > 0) location = (uint)ticket.lot_id; // job lot or unowned
                        else
                        {
                            var lot = da.Lots.Get(ticket.lot_id);
                            if (lot == null)
                            {
                                rawSession.Write(new FSOVMProtocolMessage(true, "25", "24"));
                                rawSession.Close();
                                return;
                            }
                            location = lot.location;
                        }

                        //We need to claim a lock for the avatar, if we can't do that we cant let them join
                        var didClaim = da.AvatarClaims.Claim(ticket.avatar_claim_id, ticket.avatar_claim_owner, Config.Call_Sign, location);
                        if (!didClaim)
                        {
                            rawSession.Write(new FSOVMProtocolMessage(true, "6", "26"));
                            rawSession.Close();
                            return;
                        }


                        //Time to upgrade to a voltron session
                        var newSession = Sessions.UpgradeSession<VoltronSession>(rawSession, x =>
                        {
                            rawSession.IsAuthenticated = true;
                            x.UserId = ticket.user_id;
                            x.AvatarId = ticket.avatar_id;
                            x.Authenticate(packet.Password);
                            x.AvatarClaimId = ticket.avatar_claim_id;
                        });

                        newSession.SetAttribute("cityCallSign", ticket.avatar_claim_owner);

                        if (packet.ServiceIdent == "JLT")
                        {
                            // Join lot with transition
                            // A followup request with the transition will be made.
                            newSession.SetAttribute("joinLotTransition", true);
                            newSession.SetAttribute("joinLotId", ticket.lot_id);

                            // If the session hasn't tried to join the lot in 5 seconds, close it.
                            Task.Delay(5000).ContinueWith((task) =>
                            {
                                if ((bool)newSession.GetAttribute("joinLotTransition"))
                                {
                                    newSession.SetAttribute("joinLotTransition", false);

                                    ReturnClaim(newSession);
                                }
                            });

                            return;
                        }

                        //Try and join the lot, no reason to keep this connection alive if you can't get in
                        if (!Lots.TryJoin(ticket.lot_id, newSession))
                        {
                            ReturnClaim(newSession);
                        }
                        return;
                    }
                }
            }

            //Failed authentication
            rawSession.Close();
        }

        private void ReturnClaim(IVoltronSession voltronSession)
        {
            voltronSession.Close();
            using (var db = DAFactory.Get())
            {
                //return claim to the city we got it from.
                db.AvatarClaims.Claim(voltronSession.AvatarClaimId, Config.Call_Sign, (string)voltronSession.GetAttribute("cityCallSign"), 0);
            }
        }

        public void HandleTransition(IVoltronSession voltronSession, JoinLotWithTransitionRequest joinTransition)
        {
            var transition = voltronSession.GetAttribute("joinLotTransition");
            if (transition != null && (bool)transition)
            {
                var lot_id = (int)voltronSession.GetAttribute("joinLotId");

                voltronSession.SetAttribute("joinLotTransition", false);
                voltronSession.SetAttribute("lotTransitionInfo", joinTransition.Transition);

                if (!Lots.TryJoin(lot_id, voltronSession))
                {
                    ReturnClaim(voltronSession);
                }
            }
        }

        protected override void RouteMessage(IAriesSession session, object message)
        {
            if(session is IVoltronSession voltronSession)
            {
                if (message is JoinLotWithTransitionRequest joinTransition)
                {
                    HandleTransition(voltronSession, joinTransition);
                    return;
                }

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
            LotLivenessTimer.Stop();
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
