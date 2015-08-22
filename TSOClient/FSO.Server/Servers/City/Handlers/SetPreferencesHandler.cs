using FSO.Server.Database.DA.Shards;
using FSO.Server.Framework.Aries;
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

        public void Handle(IAriesSession session, SetIgnoreListPDU packet)
        {
            session.Write(new SetIgnoreListResponsePDU {
                StatusCode = 0,
                ReasonText = "OK",
                MaxNumberOfIgnored = 50
            });
        }

        public void Handle(IAriesSession session, SetInvinciblePDU packet)
        {

        }
    }
}
