using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
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

        public List<User> GetByRegisterIP(string ip)
        {
            return Context.Connection.Query<User>("SELECT * FROM fso_users WHERE register_ip = @ip ORDER BY register_date DESC", new { ip = ip }).AsList();
        }

        public void UpdateConnectIP(uint id, string ip)
        {
            Context.Connection.Execute("UPDATE fso_users SET last_ip = @ip WHERE user_id = @user_id", new { user_id = id, ip = ip });
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
    }
}
