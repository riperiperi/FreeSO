using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

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
    }
}
