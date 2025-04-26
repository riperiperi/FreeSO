using Dapper;
using System.Linq;

namespace FSO.Server.Database.DA.ArchiveUsers
{
    internal class SqlArchiveUsers : AbstractSqlDA, IArchiveUsers
    {
        public SqlArchiveUsers(ISqlContext context) : base(context)
        {
        }

        public ArchiveUser GetByClientHash(string client_hash)
        {
            return Context.Connection.Query<ArchiveUser>("SELECT * FROM fso_users WHERE username = @client_hash", new { client_hash }).FirstOrDefault();
        }

        public ArchiveUser GetByDisplayName(string display_name)
        {
            return Context.Connection.Query<ArchiveUser>("SELECT * FROM fso_users WHERE display_name = @display_name", new { display_name }).FirstOrDefault();
        }

        public void UpdateDisplayName(uint id, string display_name)
        {
            Context.Connection.Execute("UPDATE fso_users SET display_name = @display_name WHERE user_id = @user_id", new { user_id = id, display_name = display_name });
        }

        public uint Create(ArchiveUser user)
        {
            return Context.Connection.Query<uint>(Context.CompatLayer(
                "insert into fso_users (username, email, register_date, register_ip, last_ip, is_admin, is_moderator, is_banned, display_name, is_verified, shared_user)" +
                " VALUES (@username, @email, @register_date, @register_ip, @last_ip, @is_admin, @is_moderator, @is_banned, @display_name, @is_verified, @shared_user); select LAST_INSERT_ID();"),
                user
            ).First();
        }
    }
}
