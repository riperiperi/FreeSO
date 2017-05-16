using Dapper;
using FSO.Server.Database.DA.Bans;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
    }
}
