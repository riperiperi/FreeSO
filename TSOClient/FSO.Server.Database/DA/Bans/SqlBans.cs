using Dapper;
using System.Linq;

namespace FSO.Server.Database.DA.Bans
{
    public class SqlBans : AbstractSqlDA, IBans
    {
        public SqlBans(ISqlContext context) : base(context)
        {
        }

        public DbBan GetByIP(string ip)
        {
            return Context.Connection.Query<DbBan>("SELECT * FROM fso_ip_ban WHERE ip_address = @ip", new { ip = ip }).FirstOrDefault();
        }

        /// <summary>
        /// Finds a ban by MAC Address.
        /// </summary>
        /// <param name="client_id"></param>
        /// <returns></returns>
        public DbBan GetByClientId(string client_id)
        {
            return Context.Connection.Query<DbBan>("SELECT * FROM fso_ip_ban WHERE client_id = @client_id", new { client_id = client_id }).FirstOrDefault();
        }

        public void Add(string ip, uint userid, string reason, int enddate, string client_id)
        {
            Context.Connection.Execute(
                "REPLACE INTO fso_ip_ban (user_id, ip_address, banreason, end_date, client_id) " +
                "VALUES (@user_id, @ip_address, @banreason, @end_date, @client_id)",
                new
                {
                    user_id = userid,
                    ip_address = ip,
                    banreason = reason,
                    end_date = enddate
                }
            );
        }

        /// <summary>
        /// Remove ban by user_id.
        /// </summary>
        /// <param name="ip"></param>
        public void Remove(uint user_id)
        {
            Context.Connection.Query("DELETE FROM fso_ip_ban WHERE user_id = @user_id", new { user_id = user_id });
        }
    }
}
