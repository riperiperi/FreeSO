using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Voltron.Packets;

namespace FSO.Server.Servers.City.Handlers
{
    public class FindPlayerHandler
    {
        public void Handle(IVoltronSession session, FindPlayerPDU packet)
        {
            session.Write(new FindPlayerResponsePDU {
                StatusCode = 0x00,
                ReasonText = "uh"
            });
        }
    }
}
