using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Framework;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Packets;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;

namespace FSO.Server.Servers.City.Handlers
{
    public class GlobalChatHandler
    {
        private readonly IDAFactory DAFactory;
        private readonly ISessions Sessions;
        private readonly ConcurrentDictionary<uint, DateTime> _rateLimiter = new ConcurrentDictionary<uint, DateTime>();
        private static readonly TimeSpan RateWindow = TimeSpan.FromSeconds(2);

        public GlobalChatHandler(IDAFactory daFactory, ISessions sessions)
        {
            DAFactory = daFactory;
            Sessions = sessions;
        }

        public void Handle(IVoltronSession session, GlobalChatMessage packet)
        {
            if (session.IsAnonymous) return;

            var now = DateTime.UtcNow;
            if (_rateLimiter.TryGetValue(session.AvatarId, out var lastSent) && now - lastSent < RateWindow)
                return;
            _rateLimiter[session.AvatarId] = now;

            var message = (packet.Message ?? "").Trim();
            if (string.IsNullOrEmpty(message)) return;
            if (message.Length > 200) message = message.Substring(0, 200);

            string avatarName;
            using (var db = DAFactory.Get())
            {
                var avatar = db.Avatars.Get(session.AvatarId);
                if (avatar == null) return;
                avatarName = avatar.name;
            }

            var color = packet.Color | 0xFF000000u;

            BroadcastAndPublish(session.AvatarId, avatarName, message, color);
        }

        public void BroadcastAndPublish(uint senderUID, string senderName, string message, uint color)
        {
            Sessions.GetOrCreateGroup(Groups.VOLTRON).Broadcast(new GlobalChatMessage
            {
                SenderUID = senderUID,
                SenderName = senderName,
                Message = message,
                Color = color
            });

            ChatBroadcast.Instance?.Publish(JsonConvert.SerializeObject(new
            {
                source = "global",
                avatar_id = senderUID,
                avatar_name = senderName,
                message,
                timestamp = DateTime.UtcNow.ToString("o")
            }));
        }
    }
}