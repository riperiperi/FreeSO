using FSO.Server.Database.DA;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Voltron.Packets;
using FSO.Server.Servers.Lot.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Lot.Handlers
{
    public class VoltronConnectionLifecycleHandler : IAriesSessionInterceptor
    {
        private LotHost Lots;
        private IDAFactory DAFactory;

        public VoltronConnectionLifecycleHandler(LotHost lots, IDAFactory da)
        {
            this.Lots = lots;
            this.DAFactory = da;
        }

        public void Handle(IVoltronSession session, ClientByePDU packet)
        {
            session.Close();
        }

        public async void SessionClosed(IAriesSession session)
        {
            if (!(session is IVoltronSession))
            {
                return;
            }

            IVoltronSession voltronSession = (IVoltronSession)session;
            Lots.SessionClosed(voltronSession);

        }

        public void SessionCreated(IAriesSession session)
        {
        }

        public void SessionMigrated(IAriesSession session)
        {
            if (!(session is IVoltronSession))
            {
                return;
            }
            IVoltronSession voltronSession = (IVoltronSession)session;
            Lots.SessionMigrated(voltronSession);
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
        }
    }
}
