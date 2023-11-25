using FSO.Common.Domain.Shards;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.AvatarClaims;
using FSO.Server.Database.DA.Hosts;
using FSO.Server.Domain;
using FSO.Server.Framework;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Aries.Packets;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.Server.Servers.City.Domain;
using FSO.Server.Servers.City.Handlers;
using FSO.Server.Servers.Shared.Handlers;
using Ninject;
using NLog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City
{
    public class CityServer : AbstractAriesServer
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private CityServerConfiguration Config;
        private ISessionGroup VoltronSessions;
        private CityLivenessEngine Liveness;
        public bool ShuttingDown;

        public CityServer(CityServerConfiguration config, IKernel kernel) : base(config, kernel)
        {
            this.UnexpectedDisconnectWaitSeconds = 30;
            this.TimeoutIfNoAuth = config.Timeout_No_Auth;
            this.Config = config;
            VoltronSessions = Sessions.GetOrCreateGroup(Groups.VOLTRON);
        }

        public override void Start()
        {
            LOG.Info("Starting city server for city: " + Config.ID);
            base.Start();

            Liveness.Start();
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
            Kernel.Bind<EventSystem>().ToSelf().InSingletonScope();
            Kernel.Bind<CityLivenessEngine>().ToSelf().InSingletonScope();
            Kernel.Bind<CityServerContext>().ToConstant(context);
            Kernel.Bind<int>().ToConstant(shard.Id).Named("ShardId");
            Kernel.Bind<CityServerConfiguration>().ToConstant(Config);
            Kernel.Bind<JobMatchmaker>().ToSelf().InSingletonScope();
            Kernel.Bind<LotServerPicker>().To<LotServerPicker>().InSingletonScope();
            Kernel.Bind<LotAllocations>().To<LotAllocations>().InSingletonScope();
            Kernel.Bind<Neighborhoods>().ToSelf().InSingletonScope();
            Kernel.Bind<Tuning>().ToSelf().InSingletonScope();

            Liveness = Kernel.Get<CityLivenessEngine>();

            IDAFactory da = Kernel.Get<IDAFactory>();
            using (var db = da.Get()){
                var version = ServerVersion.Get();
                db.Shards.UpdateVersion(shard.Id, version.Name, version.Number, version.UpdateID);
                ((Shards)shards).Update();

                var oldClaims = db.LotClaims.GetAllByOwner(context.Config.Call_Sign).ToList();
                if(oldClaims.Count > 0)
                {
                    LOG.Warn("Detected " + oldClaims.Count + " previously allocated lot claims, perhaps the server did not shut down cleanly. Lot consistency may be affected.");
                    db.LotClaims.RemoveAllByOwner(context.Config.Call_Sign);
                }

                var oldAvatarClaims = db.AvatarClaims.GetAllByOwner(context.Config.Call_Sign).ToList();
                if(oldAvatarClaims.Count > 0)
                {
                    LOG.Warn("Detected " + oldAvatarClaims.Count + " avatar claims, perhaps the server did not shut down cleanly. Avatar consistency may be affected.");
                    db.AvatarClaims.DeleteAll(context.Config.Call_Sign);
                }
            }

            base.Bootstrap();

            Kernel.Get<EventSystem>().Init();
        }

        public override void Shutdown()
        {
            Shutdown(ShutdownType.SHUTDOWN).RunSynchronously();
        }

        public async Task<bool> Shutdown(ShutdownType type)
        {
            Liveness.Stop();
            ShuttingDown = true;
            var lotServers = Kernel.Get<LotServerPicker>();
            var task = lotServers.ShutdownAllLotServers(type);
            await Task.WhenAny(task, Task.Delay(30 * 1000)); //wait at most 30 seconds for a city server shutdown.
            base.Shutdown();
            SignalInternalShutdown(type);
            return task.Result;
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

                using (var da = DAFactory.Get())
                {
                    var ticket = da.Shards.GetTicket(packet.Password);
                    if (ticket != null)
                    {
                        //TODO: Check if its expired
                        da.Shards.DeleteTicket(packet.Password);

                        int? claim = 0;
                        //We need to lock this avatar
                        if (ticket.avatar_id != 0) //if is 0, we're "anonymous", which limits what we can do to basically only CAS.
                        {
                            claim = da.AvatarClaims.TryCreate(new DbAvatarClaim
                            {
                                avatar_id = ticket.avatar_id,
                                location = 0,
                                owner = Config.Call_Sign
                            });

                            if (!claim.HasValue)
                            {
                                //Try and disconnect this user, if we still can't get a claim out of luck
                                //The voltron session close should handle removing any lot tickets and disconnecting them from the target servers
                                //then it will remove the avatar claim. This takes time but it should be less than 5 seconds.
                                var existingSession = Sessions.GetByAvatarId(ticket.avatar_id);
                                if (existingSession != null)
                                {
                                    existingSession.Close();
                                }
                                else
                                {
                                    //check if there really is an old claim
                                    var oldClaim = da.AvatarClaims.GetByAvatarID(ticket.avatar_id);
                                    if (oldClaim != null)
                                    {
                                        da.AvatarClaims.Delete(oldClaim.avatar_claim_id, Config.Call_Sign);
                                        LOG.Debug("Zombie Avatar claim removed: Avatar ID " + ticket.avatar_id);
                                    } else
                                    {
                                        LOG.Debug("Unknown claim error occurred. Connection will likely time out. Avatar ID " + ticket.avatar_id);
                                    }
                                }

                                //TODO: Broadcast to lot servers to disconnect

                                int i = 0;
                                while (i < 10)
                                {
                                    claim = da.AvatarClaims.TryCreate(new DbAvatarClaim
                                    {
                                        avatar_id = ticket.avatar_id,
                                        location = 0,
                                        owner = Config.Call_Sign
                                    });

                                    if (claim.HasValue)
                                    {
                                        break;
                                    }

                                    Thread.Sleep(500);
                                    i++;
                                }

                                if (!claim.HasValue)
                                {
                                    //No luck
                                    session.Close();
                                    return;
                                }
                            }
                        }

                        //Time to upgrade to a voltron session
                        var newSession = Sessions.UpgradeSession<VoltronSession>(rawSession, x => {
                            x.UserId = ticket.user_id;
                            x.AvatarId = ticket.avatar_id;
                            rawSession.IsAuthenticated = true;
                            x.Authenticate(packet.Password);
                            x.AvatarClaimId = claim.Value;
                        });
                        return;
                    }
                }
            }

            //Failed authentication
            rawSession.Close();
        }

        protected override DbHost CreateHost()
        {
            var host = base.CreateHost();
            host.role = DbHostRole.city;
            host.shard_id = Config.ID;
            return host;
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
                typeof(GluonAuthenticationHandler),
                typeof(LotServerLifecycleHandler),
                typeof(LotServerClosedownHandler),
                typeof(MessagingHandler),
                typeof(JoinLotHandler),
                typeof(LotServerShutdownResponseHandler),
                typeof(ElectronFindAvatarHandler),
                typeof(ChangeRoommateHandler),
                typeof(ModerationHandler),
                typeof(AvatarRetireHandler),
                typeof(MailHandler),
                typeof(MatchmakerNotifyHandler),
                typeof(NhoodHandler),
                typeof(BulletinHandler)
            };
        }
    }
}
