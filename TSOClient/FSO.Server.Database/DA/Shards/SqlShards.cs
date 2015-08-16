using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace FSO.Server.Database.DA.Shards
{
    public class SqlShards : AbstractSqlDA, IShards
    {
        public SqlShards(ISqlContext context) : base(context) {
        }

        public List<Shard> All()
        {
            return Context.Connection.Query<Shard>("SELECT * FROM fso_shards").ToList();
        }
    }
}
