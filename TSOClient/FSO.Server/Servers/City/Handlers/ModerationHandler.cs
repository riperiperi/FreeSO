using FSO.Server.Database.DA;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;

namespace FSO.Server.Servers.City.Handlers
{
    public class ModerationHandler
    {
        private ISessions Sessions;
        private IDAFactory DAFactory;
        private CityServerContext Context;
        public ModerationHandler(IDAFactory da, ISessions sessions, CityServerContext context)
        {
            this.DAFactory = da;
            this.Context = context;
            this.Sessions = sessions;
        }

        public void Handle(IVoltronSession session, ModerationRequest packet)
        {
            if (session.IsAnonymous) return;
            using (var da = DAFactory.Get())
            {
                var user = da.Users.GetById(session.UserId);
                var mod = user.is_moderator;
                var admin = user.is_admin;

                if (!(mod || admin)) return;

                switch (packet.Type)
                {
                    case ModerationRequestType.IPBAN_USER:
                    case ModerationRequestType.BAN_USER:
                    case ModerationRequestType.KICK_USER:
                        var targetAvatar = packet.EntityId;
                        if (packet.Type == ModerationRequestType.BAN_USER || packet.Type == ModerationRequestType.IPBAN_USER)
                        {
                            var ava = da.Avatars.Get(packet.EntityId);
                            if (ava != null)
                            {
                                var theiruser = da.Users.GetById(ava.user_id);

                                if (theiruser != null && !(theiruser.is_admin || theiruser.is_moderator))
                                {
                                    // TODO: ask the core API server to send ban mail
                                    // FSO.Server.Api.Api.INSTANCE?.SendBanMail(theiruser.username, theiruser.email, 0); // Need to handle end_date in the future

                                    da.Users.UpdateBanned(theiruser.user_id, true);
                                    if (packet.Type == ModerationRequestType.IPBAN_USER && theiruser.last_ip != "127.0.0.1" && theiruser.last_ip != "::1")
                                    {
                                        da.Bans.Add(theiruser.last_ip, theiruser.user_id, "Banned from ingame", 0, theiruser.client_id);
                                    }
                                } else
                                {
                                    return;
                                }
                            }
                        }

                        Sessions.GetByAvatarId(targetAvatar)?.Close();

                        break;
                }
            }
        }
    }
}
