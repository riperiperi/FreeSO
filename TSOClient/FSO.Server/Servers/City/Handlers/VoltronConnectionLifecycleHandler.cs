using FSO.Server.DataService.Avatars;
using FSO.Server.Framework;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Voltron.DataService;
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
        private AvatarsDataService Avatars;
        private cTSOSerializer Serializer;

        public VoltronConnectionLifecycleHandler(ISessions sessions, AvatarsDataService avatars, cTSOSerializer serializer)
        {
            this.VoltronSessions = sessions.GetOrCreateGroup(Groups.VOLTRON);
            this.Avatars = avatars;
            this.Serializer = serializer;
        }

        public void Handle(IVoltronSession session, ClientByePDU packet)
        {
            session.Close();
        }

        public void SessionClosed(IAriesSession session)
        {
            if (!(session is IVoltronSession))
            {
                return;
            }

            IVoltronSession voltronSession = (IVoltronSession)session;
            VoltronSessions.UnEnroll(session);
            Avatars.Get(voltronSession.AvatarId).Avatar_IsOnline = false;
        }

        public void SessionCreated(IAriesSession session)
        {
        }

        public void SessionUpgraded(IAriesSession oldSession, IAriesSession newSession)
        {
            if (!(newSession is IVoltronSession))
            {
                return;
            }

            //Aries session has upgraded to a voltron session
            IVoltronSession voltronSession = (IVoltronSession)newSession;

            //TODO: Make sure this user is not already connected, if they are disconnect them


            //New avatar, enroll in voltron group
            var avatar = Avatars.Get(voltronSession.AvatarId);

            //Mark as online
            avatar.Avatar_IsOnline = true;
            
            VoltronSessions.Enroll(newSession);

            newSession.Write(new HostOnlinePDU
            {
                ClientBufSize = 4096,
                HostVersion = 0x7FFF,
                HostReservedWords = 0
            });

            //TODO: Somehow alert people this sim is online?
        }
    }
}
