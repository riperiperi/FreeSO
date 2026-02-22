using FSO.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Avatars;
using FSO.Server.Database.DA.Bans;
using FSO.Server.Database.DA.Users;
using FSO.Server.Protocol.Embedded;

namespace FSO.Server.Embedded
{
    public class ArchiveManagement
    {
        private IDAFactory DAFactory;

        public ArchiveManagement(ArchiveConfiguration config)
        {
            var sConfig = ArchiveConfigBuilder.Build(config);

            DAFactory = new SqliteDAFactory(sConfig.Database);
        }

        private ArchiveDbAvatar AvatarFromSummary(DbAvatarSummary summary)
        {
            return new ArchiveDbAvatar()
            {
                ID = summary.avatar_id,
                Name = summary.name,
                LotName = summary.lot_name,
                LastLogin = 0
            };
        }

        private ArchiveDbUser UserFromSummary(UserSummary summary)
        {
            ArchiveDbUserStatus status;

            if (summary.is_banned)
            {
                status = ArchiveDbUserStatus.Banned;
            }
            else if (!summary.is_verified) // todo: server doesn't need verification?
            {
                status = ArchiveDbUserStatus.Unverified;
            }
            else if (summary.is_admin)
            {
                status = ArchiveDbUserStatus.Admin;
            }
            else if (summary.is_moderator)
            {
                status = ArchiveDbUserStatus.Mod;
            }
            else
            {
                status = ArchiveDbUserStatus.Normal;
            }

            return new ArchiveDbUser()
            {
                ID = summary.user_id,
                AvatarCount = summary.avatar_count,
                Status = status,
                Name = summary.display_name,
                IP = summary.last_ip,
            };
        }

        private ArchiveDbIpBan BanFromDb(DbBan ban)
        {
            return new ArchiveDbIpBan()
            {
                IP = ban.ip_address
            };
        }

        public List<ArchiveDbUser> GetUsers()
        {
            using (var da = DAFactory.Get())
            {
                var users = da.Users.AllSummaries();

                return [.. users.Select(UserFromSummary)];
            }
        }

        public List<ArchiveDbAvatar> GetAvatars(uint userId)
        {
            using (var da = DAFactory.Get())
            {
                var avatars = da.Avatars.GetSummaryByUserId(userId);

                return [.. avatars.Select(AvatarFromSummary)];
            }
        }

        public List<ArchiveDbIpBan> GetIpBans()
        {
            using (var da = DAFactory.Get())
            {
                var bans = da.Bans.All();

                return [.. bans.Select(BanFromDb)];
            }
        }

        public void DeleteUser(int userId)
        {
            using (var da = DAFactory.Get())
            {
                var avatars = da.Avatars.GetByUserId((uint)userId);

                foreach (var ava in avatars)
                {
                    da.Avatars.UpdateUser(ava.avatar_id, 1); // TODO: better indicator for archive user?
                }

                da.Users.Delete((uint)userId);
            }
        }

        public void BanUser(int userId)
        {
            using (var da = DAFactory.Get())
            {
                var user = da.Users.GetById((uint)userId);

                if (user == null)
                {
                    return;
                }

                var existingBan = da.Bans.GetByIP(user.last_ip);

                if (existingBan == null)
                {
                    BanIp(user.last_ip);
                }
            }
        }

        public void UnbanUser(int userId)
        {
            using (var da = DAFactory.Get())
            {
                var user = da.Users.GetById((uint)userId);

                if (user == null)
                {
                    return;
                }

                da.Bans.Remove((uint)userId);
                da.Bans.RemoveByIp(user.last_ip);
            }
        }

        public void DeleteAvatar(int avatarId)
        {
            using (var da = DAFactory.Get())
            {
                // TODO: safety stuff (does sqlite properly cascade anything?)
                da.Avatars.Delete((uint)avatarId);
            }
        }

        public void MigrateAvatar(int avatarId, int userId)
        {
            using (var da = DAFactory.Get())
            {
                da.Avatars.UpdateUser((uint)avatarId, (uint)userId);
            }
        }

        public void BanIp(string ip)
        {
            using (var da = DAFactory.Get())
            {
                var relatedUsers = da.Users.GetByLastIP(ip);

                uint userId = 0;

                foreach (var user in relatedUsers)
                {
                    da.Users.UpdateBanned(user.user_id, true);
                    userId = user.user_id;
                }

                da.Bans.Add(ip, userId, "Archive management", 0, "");
            }
        }

        public void UnbanIp(string ip)
        {
            using (var da = DAFactory.Get())
            {
                da.Bans.RemoveByIp(ip);

                var users = da.Users.GetByLastIP(ip);

                foreach (var user in users)
                {
                    da.Users.UpdateBanned(user.user_id, false);
                }
            }
        }

        public List<ArchiveDbUser> GetUsersForIp(string ip)
        {
            using (var da = DAFactory.Get())
            {
                var users = da.Users.AllSummaries().Where((x) => x.last_ip == ip);

                return [.. users.Select(UserFromSummary)];
            }
        }

        public void SetInfo(string name, string map)
        {
            using (var da = DAFactory.Get())
            {
                da.Shards.UpdateInfo(1, name, map);
            }
        }
    }
}
