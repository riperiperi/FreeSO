using System.Collections.Generic;
using System.Linq;
using Dapper;
using FSO.Server.Common;
using FSO.Server.Database.DA.Utils;

namespace FSO.Server.Database.DA.Users
{
    public class SqlUsers : AbstractSqlDA, IUsers
    {
        public SqlUsers(ISqlContext context) : base(context)
        {
        }

        public User GetByUsername(string username)
        {
            return Context.Connection.Query<User>("SELECT * FROM fso_users WHERE username = @username", new { username = username }).FirstOrDefault();
        }

        public UserAuthenticate GetAuthenticationSettings(uint userId)
        {
            return Context.Connection.Query<UserAuthenticate>("SELECT * FROM fso_user_authenticate WHERE user_id = @user_id", new { user_id = userId }).FirstOrDefault();
        }

        public User GetById(uint id)
        {
            return Context.Connection.Query<User>("SELECT * FROM fso_users WHERE user_id = @user_id", new { user_id = id }).FirstOrDefault();
        }

        public User GetByEmail(string email)
        {
            return Context.Connection.Query<User>("SELECT * FROM fso_users WHERE email = @email", new { email = email }).FirstOrDefault();
        }
        public List<User> GetByRegisterIP(string ip)
        {
            return Context.Connection.Query<User>("SELECT * FROM fso_users WHERE register_ip = @ip ORDER BY register_date DESC", new { ip = ip }).AsList();
        }

        public void UpdateConnectIP(uint id, string ip)
        {
            Context.Connection.Execute("UPDATE fso_users SET last_ip = @ip WHERE user_id = @user_id", new { user_id = id, ip = ip });
        }

        public void UpdateClientID(uint id, string uid)
        {
            Context.Connection.Execute("UPDATE fso_users SET client_id = @id WHERE user_id = @user_id", new { user_id = id, id = uid });
        }

        public void UpdateBanned(uint id, bool banned)
        {
            Context.Connection.Execute("UPDATE fso_users SET is_banned = @ban WHERE user_id = @user_id", new { user_id = id, ban = banned });
        }

        public void UpdateLastLogin(uint id, uint last_login)
        {
            Context.Connection.Execute("UPDATE fso_users SET last_login = @last_login WHERE user_id = @user_id", new { user_id = id, last_login = last_login });
        }

        public PagedList<User> All(int offset = 1, int limit = 20, string orderBy = "register_date")
        {
            var connection = Context.Connection;
            var total = connection.Query<int>("SELECT COUNT(*) FROM fso_users").FirstOrDefault();
            var results = connection.Query<User>("SELECT * FROM fso_users ORDER BY @order LIMIT @offset, @limit", new { order = orderBy, offset = offset, limit = limit });
            return new PagedList<User>(results, offset, total);
        }

        public uint Create(User user)
        {
            return Context.Connection.Query<uint>(
                "insert into fso_users set username = @username, email = @email, register_date = @register_date, register_ip = @register_ip, last_ip = @last_ip, " + 
                "is_admin = @is_admin, is_moderator = @is_moderator, is_banned = @is_banned; select LAST_INSERT_ID();",
                user
            ).First();
        }

        public void CreateAuth(UserAuthenticate auth)
        {
            Context.Connection.Execute(
                "insert into fso_user_authenticate set user_id = @user_id, scheme_class = @scheme_class, data = @data;",
                auth
            );
        }

        public void UpdateAuth(UserAuthenticate auth)
        {
            Context.Connection.Execute(
                "UPDATE fso_user_authenticate SET scheme_class = @scheme_class, data = @data WHERE user_id = @user_id;",
                new { auth.scheme_class, auth.data, auth.user_id }
            );
        }

        public DbAuthAttempt GetRemainingAuth(uint user_id, string ip)
        {
            var result = Context.Connection.Query<DbAuthAttempt>(
                "SELECT * FROM fso_auth_attempts WHERE user_id = @user_id AND ip = @ip AND invalidated = 0 ORDER BY expire_time DESC",
                new { user_id, ip }
                ).FirstOrDefault();
            if (result != null && result.active && result.expire_time < Epoch.Now) return null;
            else return result;
        }

        public int FailedConsecutive(uint user_id, string ip)
        {
            return Context.Connection.Query<int>(
            "SELECT COUNT(*) FROM fso_auth_attempts WHERE user_id = @user_id AND ip = @ip AND active = 1 AND invalidated = 0",
            new { user_id, ip }
            ).First();
        }

        public int FailedAuth(uint attempt_id, uint delay, int failLimit)
        {
            Context.Connection.Execute(
                "UPDATE fso_auth_attempts SET count = count + 1, expire_time = @time WHERE attempt_id = @attempt_id",
                new { attempt_id, time = Epoch.Now + delay }
            );
            var result = Context.Connection.Query<DbAuthAttempt>(
                "SELECT * FROM fso_auth_attempts WHERE attempt_id = @attempt_id", new { attempt_id }).FirstOrDefault();
            if (result != null)
            {
                if (result.count >= failLimit)
                {
                    Context.Connection.Execute(
                        "UPDATE fso_auth_attempts SET active = 1 WHERE attempt_id = @attempt_id",
                        new { attempt_id });
                    return 0;
                } else
                {
                    return failLimit - result.count;
                }
            } else
            {
                return failLimit;
            }
        }

        public void NewFailedAuth(uint user_id, string ip, uint delay)
        {
            //create a new entry
            Context.Connection.Execute(
                "INSERT INTO fso_auth_attempts (ip, user_id, expire_time, count) VALUES (@ip, @user_id, @time, 1)",
                new { user_id, ip, time = Epoch.Now + delay }
            );
        }

        public void SuccessfulAuth(uint user_id, string ip)
        {
            Context.Connection.Execute(
                "UPDATE fso_auth_attempts SET invalidated = 1 WHERE user_id = @user_id AND ip = @ip",
                new { user_id, ip }
            );
        }
    }
}
