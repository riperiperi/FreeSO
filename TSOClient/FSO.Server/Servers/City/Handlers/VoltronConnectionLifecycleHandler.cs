using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Server.Database.DA;
using FSO.Server.Framework;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Voltron.Packets;
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

        public VoltronConnectionLifecycleHandler(ISessions sessions, IDataService dataService, IDAFactory da, CityServerContext context)
        {
            this.VoltronSessions = sessions.GetOrCreateGroup(Groups.VOLTRON);
            this.DataService = dataService;
            this.DAFactory = da;
            this.Context = context;
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

            //New avatar, enroll in voltron group
            var avatar = await DataService.Get<Avatar>(voltronSession.AvatarId);
            //Mark as online
            avatar.Avatar_IsOnline = false;
            VoltronSessions.UnEnroll(session);

            using (var db = DAFactory.Get())
            {
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
