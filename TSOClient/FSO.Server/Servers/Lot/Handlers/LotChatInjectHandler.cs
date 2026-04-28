using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Servers.Lot.Domain;
using NLog;

namespace FSO.Server.Servers.Lot.Handlers
{
    public class LotChatInjectHandler
    {
        private static Logger LOG = LogManager.GetCurrentClassLogger();
        private LotHost Lots;

        public LotChatInjectHandler(LotHost lots)
        {
            this.Lots = lots;
        }

        public void Handle(IGluonSession session, InjectLotChatPacket packet)
        {
            var injected = Lots.InjectDiscordMessage(
                (int)packet.LotId,
                packet.AvatarName ?? "",
                packet.Message ?? "");

            if (!injected)
                LOG.Debug("InjectLotChat: lot {0} not found on this server", packet.LotId);
        }
    }
}