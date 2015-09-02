using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Voltron.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.City.Handlers
{
    public class FindPlayerHandler
    {
        public void Handle(IVoltronSession session, FindPlayerPDU packet)
        {
            session.Write(new FindPlayerResponsePDU {
                StatusCode = 0x01,
                ReasonText = ""
            });
        }
    }
}
