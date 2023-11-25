using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using FSO.Common.Enum;

namespace FSO.Server.Database.DA.LotTop100
{
    public class SqlLotTop100 : AbstractSqlDA, ILotTop100
    {
        public SqlLotTop100(ISqlContext context) : base(context)
        {
        }

        public IEnumerable<DbLotTop100> All()
        {
            return Context.Connection.Query<DbLotTop100>("SELECT top.*, l.name as lot_name, l.location as lot_location FROM fso_lot_top_100 top LEFT JOIN fso_lots l ON top.lot_id = l.lot_id");
        }

        public bool Calculate(DateTime date, int shard_id)
        {
            try
            {
                Context.Connection.Execute("CALL fso_lot_top_100_calc_all(@date, @shard_id);", new { date = date, shard_id = shard_id });
                return true;
            }catch(Exception ex)
            {
                return false;
            }
        }
        public IEnumerable<DbLotTop100> GetAllByShard(int shard_id)
        {
            return Context.Connection.Query<DbLotTop100>("SELECT top.*, l.name as lot_name, l.location as lot_location FROM fso_lot_top_100 top LEFT JOIN fso_lots l ON top.lot_id = l.lot_id WHERE top.shard_id = @shard_id", new
            {
                shard_id = shard_id
            });
        }
        public IEnumerable<DbLotTop100> GetByCategory(int shard_id, LotCategory category)
        {
            return Context.Connection.Query<DbLotTop100>("SELECT top.*, l.name as lot_name, l.location as lot_location FROM fso_lot_top_100 top LEFT JOIN fso_lots l ON top.lot_id = l.lot_id WHERE top.category = @category AND top.shard_id = @shard_id", new
            {
                category = category.ToString(),
                shard_id = shard_id
            });
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
