using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Server.Database.DA;
using FSO.Server.Framework;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.Server.Servers.City.Domain;

namespace FSO.Server.Servers.City.Handlers
{
    public class VoltronConnectionLifecycleHandler : IAriesSessionInterceptor
    {
        private ISessionGroup VoltronSessions;
        private ISessions Sessions;
        private IDataService DataService;
        private IDAFactory DAFactory;
        private CityServerContext Context;
        private LotServerPicker LotServers;
        private CityLivenessEngine Liveness;
        private EventSystem Events;
        private Neighborhoods Neigh;
        private Tuning TuningDomain;

        public VoltronConnectionLifecycleHandler(ISessions sessions, IDataService dataService, IDAFactory da, CityServerContext context, LotServerPicker lotServers, CityLivenessEngine engine,
            EventSystem events, Neighborhoods neigh, Tuning tuning)
        {
            this.VoltronSessions = sessions.GetOrCreateGroup(Groups.VOLTRON);
            this.Sessions = sessions;
            this.DataService = dataService;
            this.DAFactory = da;
            this.Context = context;
            this.LotServers = lotServers;
            this.Liveness = engine;
            this.Events = events;
            this.Neigh = neigh;
            this.TuningDomain = tuning;
        }

        public void Handle(IVoltronSession session, ClientByePDU packet)
        {
            session.Close();
        }

        public async void SessionClosed(IAriesSession session)
        {
            if (!(session is IVoltronSession)) {
                return;
            }

            IVoltronSession voltronSession = (IVoltronSession)session;
            VoltronSessions.UnEnroll(session);

            if (voltronSession.IsAnonymous) return;

            Liveness.EnqueueChange(() => {
                //unenroll in voltron group, mark as offline in data service.
                //since this can happen async make sure our session hasnt been reopened before trying to delete its claim
                if (Sessions.GetByAvatarId(voltronSession.AvatarId)?.Connected == true) return;

                var avatar = DataService.Get<Avatar>(voltronSession.AvatarId).Result;
                if (avatar != null) avatar.Avatar_IsOnline = false;

                using (var db = DAFactory.Get())
                {
                    // if we don't own the claim for the avatar, we need to tell the server that does to release the avatar.
                    // right now it's just lot servers.

                    var claim = db.AvatarClaims.Get(voltronSession.AvatarClaimId);
                    if (claim != null && claim.owner != Context.Config.Call_Sign)
                    {
                        var lotServer = LotServers.GetLotServerSession(claim.owner);
                        if (lotServer != null)
                        {
                            var lot = db.Lots.GetByLocation(Context.ShardId, claim.location);
                            lotServer.Write(new RequestLotClientTermination()
                            {
                                AvatarId = voltronSession.AvatarId,
                                LotId = (lot != null) ? lot.lot_id : ((int)claim.location),
                                FromOwner = Context.Config.Call_Sign
                            });
                        }
                    }

                    //nuke the claim anyways to be sure.
                    db.AvatarClaims.Delete(voltronSession.AvatarClaimId, Context.Config.Call_Sign);
                }
            });
        }

        public void SessionCreated(IAriesSession session)
        {
        }

        public void SessionMigrated(IAriesSession session)
        {
            //on reconnect to city. nothing right now.
        }

        public async void SessionUpgraded(IAriesSession oldSession, IAriesSession newSession)
        {
            if (!(newSession is IVoltronSession))
            {
                return;
            }

            //Aries session has upgraded to a voltron session
            IVoltronSession voltronSession = (IVoltronSession)newSession;

            //TODO: Make sure this user is not already connected, if they are disconnect them
            newSession.Write(new HostOnlinePDU
            {
                ClientBufSize = 4096,
                HostVersion = 0x7FFF,
                HostReservedWords = 0
            });

            //CAS, don't hydrate the user
            if (voltronSession.IsAnonymous){
                return;
            }

            //New avatar, enroll in voltron group
            var avatar = await DataService.Get<Avatar>(voltronSession.AvatarId); //can throw?
            //Mark as online
            avatar.Avatar_IsOnline = true;
            VoltronSessions.Enroll(newSession);
            Events.UserJoined(voltronSession);
            Neigh.UserJoined(voltronSession);
            TuningDomain.UserJoined(voltronSession);

            //TODO: Somehow alert people this sim is online?
        }
    }
}
