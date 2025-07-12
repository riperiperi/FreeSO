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
                if (Context.SupportsFunctions)
                {
                    Context.Connection.Execute("CALL fso_lot_top_100_calc_all(@date, @shard_id);", new { date = date, shard_id = shard_id });
                }
                else
                {
                    CalculateCategory("money", date, shard_id);
                    CalculateCategory("offbeat", date, shard_id);
                    CalculateCategory("romance", date, shard_id);
                    CalculateCategory("services", date, shard_id);
                    CalculateCategory("shopping", date, shard_id);
                    CalculateCategory("skills", date, shard_id);
                    CalculateCategory("welcome", date, shard_id);
                    CalculateCategory("games", date, shard_id);
                    CalculateCategory("entertainment", date, shard_id);
                    CalculateCategory("residence", date, shard_id);
                }
                return true;
            }catch(Exception ex)
            {
                return false;
            }
        }

        public void CalculateCategory(string category, DateTime date, int shard_id)
        {
            var transaction = Context.Connection.BeginTransaction();

            var start_date = date - TimeSpan.FromDays(4);
            var timestamp = DateTime.Now;

            try
            {
                Context.Connection.Execute(@"DELETE FROM fso_lot_top_100 WHERE shard_id = @shard_id AND category = @category;
		INSERT INTO fso_lot_top_100 (category, rank, shard_id, lot_id, minutes, date)
			SELECT category,
                    rank,
					shard_id, 
					lot_id, 
					minutes, 
					date 
					FROM (
						SELECT lot.category, lot.lot_id, lot.shard_id, FLOOR(AVG(visits.minutes)) as minutes, @timestamp as date,
                            ROW_NUMBER () OVER (
                                    ORDER BY minutes DESC 
                                ) rank
							FROM fso_lot_visit_totals visits 
								INNER JOIN fso_lots lot ON visits.lot_id = lot.lot_id
							WHERE lot.category = @category 
								AND date BETWEEN @start_date AND @date
								AND lot.shard_id = @shard_id
							GROUP BY lot.lot_id
							ORDER BY minutes DESC
							LIMIT 100
					) as top100;", new { date, shard_id, category, timestamp, start_date });

                transaction.Commit();
            }
            catch (Exception e)
            {
                transaction.Rollback();
                throw e;
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
