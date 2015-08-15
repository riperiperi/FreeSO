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
    }
}
