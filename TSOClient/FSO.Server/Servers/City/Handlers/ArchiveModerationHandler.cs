using FSO.Common.DataService;
using FSO.Common.DataService.Model;
using FSO.Server.Database.DA;
using FSO.Server.Framework.Aries;
using FSO.Server.Framework.Voltron;
using FSO.Server.Protocol.Electron.Model;
using FSO.Server.Protocol.Electron.Packets;
using FSO.Server.Protocol.Gluon.Packets;
using FSO.Server.Servers.City.Domain;

namespace FSO.Server.Servers.City.Handlers
{
    internal class ArchiveModerationHandler
    {
        private ISessions Sessions;
        private IDAFactory DAFactory;
        private CityServerContext Context;
        private LotServerPicker LotServers;
        private IDataService DataService;

        public ArchiveModerationHandler(IDAFactory da, ISessions sessions, CityServerContext context, LotServerPicker lotServers, IDataService dataService)
        {
            this.DAFactory = da;
            this.Context = context;
            this.Sessions = sessions;
            this.LotServers = lotServers;
            this.DataService = dataService;
        }

        public void Handle(IVoltronSession session, ArchiveModerationRequest packet)
        {
            if (session.IsAnonymous) return;
            using (var da = DAFactory.Get())
            {
                var user = da.Users.GetById(session.UserId);
                var mod = user.is_moderator;
                var admin = user.is_admin;

                int myLevel = user.is_admin ? 2 : (user.is_moderator ? 1 : 0);

                if (myLevel == 0) return;

                // All requests are against users for now
                var target = da.Users.GetById(packet.EntityId);

                if (target == null) return;

                int userLevel = target.is_admin ? 2 : (target.is_moderator ? 1 : 0);

                if (userLevel >= myLevel)
                {
                    // Can't perform actions on people with a higher mod level...
                    return;
                }

                // Try and find the user sessions - this can be useful for updating user state in real time.
                var sessions = Sessions.GetAllByUserId(target.user_id);

                switch (packet.Type)
                {
                    case ArchiveModerationRequestType.BAN_USER:
                    case ArchiveModerationRequestType.KICK_USER:
                        if (packet.Type == ArchiveModerationRequestType.BAN_USER)
                        {
                            da.Users.UpdateBanned(target.user_id, true);
                            if (target.last_ip != "127.0.0.1" && target.last_ip != "::1")
                            {
                                da.Bans.Add(target.last_ip, target.user_id, "Banned from ingame", 0, target.client_id);
                            }
                        }

                        foreach (var targSession in sessions)
                        {
                            targSession?.Close();
                        }
                        break;
                    case ArchiveModerationRequestType.CHANGE_MOD_LEVEL:
                        int level = packet.Value;
                        da.Users.UpdatePermissions(target.user_id, level >= 1, level >= 2);

                        foreach (var targSession in sessions)
                        {
                            if (targSession is VoltronSession vSession)
                            {
                                vSession.ModerationLevel = (uint)level;
                            }
                        }

                        // try to notify the lot(s) if possible
                        // slightly overcomplicated...

                        foreach (var targSession in sessions)
                        {
                            var avatarId = targSession.AvatarId;

                            if (avatarId != 0)
                            {
                                // Update this sim's moderation level.
                                da.Avatars.UpdateModerationLevel(avatarId, level);
                                DataService.Invalidate<Avatar>(avatarId);

                                // Try find the lot that the avatar is on.
                                var claim = da.AvatarClaims.GetByAvatarID(avatarId);

                                if (claim != null && claim.location != 0)
                                {
                                    var lot = da.Lots.GetByLocation(Context.ShardId, claim.location);

                                    if (lot != null)
                                    {
                                        var lotOwned = da.LotClaims.GetByLotID(lot.lot_id);
                                        if (lotOwned != null)
                                        {
                                            var lotServer = LotServers.GetLotServerSession(lotOwned.owner);
                                            if (lotServer != null)
                                            {
                                                //immediately notify lot of new roommate
                                                lotServer.Write(new NotifyLotRoommateChange()
                                                {
                                                    AvatarId = avatarId,
                                                    LotId = lot.lot_id,
                                                    Change = Protocol.Gluon.Model.ChangeType.RELOAD_PERMISSIONS
                                                });
                                            }
                                        }
                                    }
                                }
                            }

                        }

                        Context.BroadcastUserList(false);

                        break;
                    case ArchiveModerationRequestType.APPROVE_USER:
                    case ArchiveModerationRequestType.REJECT_USER:
                        // If the user is already approved, we can't really do anything.
                        bool approval = packet.Type == ArchiveModerationRequestType.APPROVE_USER;

                        if (approval)
                        {
                            // Allow session to continue, record in database

                            da.Users.UpdateVerified(target.user_id, true);

                            foreach (var targSession in sessions)
                            {
                                if (targSession is VoltronSession vSession)
                                {
                                    vSession.Unverified = false;
                                    vSession.Write(new VerificationNotification()
                                    {
                                        IsVerified = true
                                    });
                                }
                            }

                            Context.BroadcastUserList(false);
                        }
                        else
                        {
                            // Close session...
                            foreach (var targSession in sessions)
                            {
                                targSession.Write(new VerificationNotification()
                                {
                                    IsVerified = false
                                });

                                targSession?.Close();
                            }
                        }
                        break;
                }
            }
        }
    }
}
