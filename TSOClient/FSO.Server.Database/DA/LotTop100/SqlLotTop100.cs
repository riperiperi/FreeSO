using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.LotTop100
{
    public class SqlLotTop100 : AbstractSqlDA, ILotTop100
    {
        public SqlLotTop100(ISqlContext context) : base(context)
        {
        }

        public void Replace(IEnumerable<DbLotTop100> values)
        {
            try {
                var valuesConverted = values.Select(x => {
                    return new
                    {
                        category = x.category.ToString(),
                        rank = x.rank,
                        shard_id = x.shard_id,
                        lot_id = x.lot_id,
                        minutes = x.minutes
                    };
                });

                Context.Connection.Execute("REPLACE INTO fso_lot_top_100 (category, rank, shard_id, lot_id, minutes) VALUES (@category, @rank, @shard_id, @lot_id, @minutes)", valuesConverted);
            }catch(Exception ex)
            {
            }
        }
    }
}
