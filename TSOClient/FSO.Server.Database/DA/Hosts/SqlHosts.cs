using Dapper;
using System;
using System.Collections.Generic;

namespace FSO.Server.Database.DA.Hosts
{
    public class SqlHosts : AbstractSqlDA, IHosts
    {
        public SqlHosts(ISqlContext context) : base(context)
        {
        }

        public IEnumerable<DbHost> All()
        {
            return Context.Connection.Query<DbHost>("SELECT * FROM fso_hosts");
        }

        public void CreateHost(DbHost host)
        {
            Context.Connection.Execute(
                "REPLACE INTO fso_hosts (call_sign, role, status, internal_host, public_host, time_boot, shard_id) " +
                "VALUES (@call_sign, @role, @status, @internal_host, @public_host, @time_boot, @shard_id)",
                new
                {
                    call_sign = host.call_sign,
                    role = host.role.ToString(),
                    status = host.status.ToString(),
                    internal_host = host.internal_host,
                    public_host = host.public_host,
                    time_boot = host.time_boot,
                    shard_id = host.shard_id
                }
            );
        }

        public DbHost Get(string call_sign)
        {
            throw new NotImplementedException();
        }

        public void SetStatus(string call_sign, DbHostStatus status)
        {
            Context.Connection.Execute("UPDATE fso_hosts SET status = @status WHERE call_sign = @call_sign", new {
                call_sign = call_sign,
                status = status.ToString()
            });
        }
    }
}
