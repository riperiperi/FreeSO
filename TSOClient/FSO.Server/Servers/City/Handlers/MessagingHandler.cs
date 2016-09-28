using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class MessagingHandler
    {
        private ISessions Sessions;

        public MessagingHandler(ISessions sessions)
        {
            this.Sessions = sessions;
        }

        public void Handle(IVoltronSession session, InstantMessage message)
        {
            if (session.IsAnonymous) //CAS users can't do this.
                return;

            var targetSession = Sessions.GetByAvatarId(message.To);
            if(targetSession == null)
            {
                session.Write(new InstantMessage {
                    FromType = FSO.Common.Enum.UserReferenceType.AVATAR,
                    From = message.To,
                    Type = InstantMessageType.FAILURE_ACK,
                    Message = "",
                    AckID = message.AckID,
                    Reason = InstantMessageFailureReason.THEY_ARE_OFFLINE
                });
                return;
            }

            targetSession.Write(message);
        }
    }
}
