using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron;

namespace FSO.Server.Servers.City
{
    public class CityServerContext
    {
        public int ShardId;
        public CityServerConfiguration Config;
        public ISessions Sessions;
        public Action<bool> BroadcastUserList;

        public void Broadcast(AbstractElectronPacket packet, Func<VoltronSession, bool> filter = null)
        {
            Task.Run(() =>
            {

                var clone = Sessions.Clone();
                foreach (var session in clone)
                {
                    if (session is VoltronSession vSession)
                    {
                        if (!vSession.IsAnonymous && (filter?.Invoke(vSession) != false))
                        {
                            vSession.Write(packet);
                        }
                    }
                }
            });
        }
    }
}
