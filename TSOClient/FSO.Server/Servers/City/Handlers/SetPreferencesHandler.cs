using FSO.Server.Database.DA.Shards;
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
    public class SetPreferencesHandler
    {
        public SetPreferencesHandler()
        {
        }

        public void Handle(IVoltronSession session, SetIgnoreListPDU packet)
        {
            session.Write(new SetIgnoreListResponsePDU {
                StatusCode = 0,
                ReasonText = "OK",
                MaxNumberOfIgnored = 50
            });
        }

        public void Handle(IVoltronSession session, SetInvinciblePDU packet)
        {
        }
    }
}
