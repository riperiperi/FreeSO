using FSO.Server.Common;
using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Gluon.Packets;
using Newtonsoft.Json;
using System;

namespace FSO.Server.Servers.City.Handlers
{
    public class LotChatNotifyHandler
    {
        public void Handle(IGluonSession session, LotChatNotify packet)
        {
            var hub = ChatBroadcast.Instance;
            if (hub == null) return;

            var json = JsonConvert.SerializeObject(new
            {
                lot_id = packet.LotId,
                lot_name = packet.LotName,
                avatar_name = packet.AvatarName,
                message = packet.Message,
                channel_id = packet.ChannelId,
                timestamp = DateTime.UtcNow.ToString("o")
            });

            hub.Publish(json);
        }
    }
}