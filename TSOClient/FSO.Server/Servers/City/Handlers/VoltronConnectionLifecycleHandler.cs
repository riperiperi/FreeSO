using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Server.Database.DA;
using FSO.Server.Framework;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.Server.Servers.City.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class VoltronConnectionLifecycleHandler : IAriesSessionInterceptor
    {
        private ISessionGroup VoltronSessions;
        private IDataService DataService;
        private IDAFactory DAFactory;
        private CityServerContext Context;
        private LotServerPicker LotServers;

        public VoltronConnectionLifecycleHandler(ISessions sessions, IDataService dataService, IDAFactory da, CityServerContext context, LotServerPicker lotServers)
        {
            this.VoltronSessions = sessions.GetOrCreateGroup(Groups.VOLTRON);
            this.DataService = dataService;
            this.DAFactory = da;
            this.Context = context;
            this.LotServers = lotServers;
        }

        public void Handle(IVoltronSession session, ClientByePDU packet)
        {
            session.Close();
        }

        public async void SessionClosed(IAriesSession session)
        {
            if (!(session is IVoltronSession)){
                return;
            }

            IVoltronSession voltronSession = (IVoltronSession)session;

            //unenroll in voltron group, mark as offline in data service.
            var avatar = await DataService.Get<Avatar>(voltronSession.AvatarId);
            avatar.Avatar_IsOnline = false;
            VoltronSessions.UnEnroll(session);

            using (var db = DAFactory.Get())
            {
                // if the avatar has a lot ticket, we must destroy it and tell the relevant server to disconnect that client
                var tickets = db.Lots.GetLotServerTicketsForClaimedAvatar(voltronSession.AvatarClaimId);
                foreach (var ticket in tickets)
                {
                    //delete this ticket. tell the server we're through.
                    //..but we need to find what server has claimed the lot the ticket is for.
                    var lotServer = LotServers.GetLotServerSession(ticket.lot_owner);
                    if (lotServer != null)
                    {
                        lotServer.Write(new RequestLotClientTermination()
                        {
                            AvatarId = voltronSession.AvatarId,
                            LotId = ticket.lot_id,
                            FromOwner = Context.Config.Call_Sign
                        });
                    }
                    db.Lots.DeleteLotServerTicket(ticket.ticket_id);
                }
                db.AvatarClaims.Delete(voltronSession.AvatarClaimId, Context.Config.Call_Sign);
            }
        }

        public void SessionCreated(IAriesSession session)
        {
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
            var avatar = await DataService.Get<Avatar>(voltronSession.AvatarId);
            //Mark as online
            avatar.Avatar_IsOnline = true;
            VoltronSessions.Enroll(newSession);

            //TODO: Somehow alert people this sim is online?
        }
    }
}
