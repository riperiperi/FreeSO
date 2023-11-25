using Dapper;
using System.Collections.Generic;
using System.Linq;
using FSO.Server.Database.DA.Tuning;
using FSO.Server.Database.DA.Utils;
using System.Data.SqlClient;

namespace FSO.Server.Database.DA.DynPayouts
{
    public class SqlDynPayouts : AbstractSqlDA, IDynPayouts
    {
        public SqlDynPayouts(ISqlContext context) : base(context)
        {
        }

        public List<DbDynPayout> GetPayoutHistory(int limitDay)
        {
            return Context.Connection.Query<DbDynPayout>("SELECT * FROM fso_dyn_payouts ORDER BY day DESC LIMIT 56", new { limitDay = limitDay }).ToList();
        }

        public List<DbTransSummary> GetSummary(int limitDay)
        {
            return Context.Connection.Query<DbTransSummary>("SELECT transaction_type, sum(value) AS value, sum(count) AS sum FROM fso.fso_transactions "
                +"WHERE transaction_type > 40 AND transaction_type < 51 AND day >= @limitDay GROUP BY transaction_type", new { limitDay = limitDay }).ToList();
        }

        public bool InsertDynRecord(List<DbDynPayout> dynPayout)
        {
            try
            {
                Context.Connection.ExecuteBufferedInsert("INSERT INTO fso_dyn_payouts (day, skilltype, multiplier, flags) VALUES (@day, @skilltype, @multiplier, @flags) ON DUPLICATE KEY UPDATE multiplier = @multiplier", dynPayout, 100);
            }
            catch (SqlException)
            {
                return false;
            }
            return true;
        }

        public bool Purge(int limitDay)
        {
            Context.Connection.Query("DELETE FROM fso_dyn_payouts WHERE day < @day", new { day = limitDay });
            return true;
        }

        public bool ReplaceDynTuning(List<DbTuning> dynTuning)
        {
            try
            {
                var deleted = Context.Connection.Execute("DELETE FROM fso_tuning WHERE owner_type = 'DYNAMIC' AND owner_id = 1");
                Context.Connection.ExecuteBufferedInsert("INSERT INTO fso_tuning (tuning_type, tuning_table, tuning_index, value, owner_type, owner_id) VALUES (@tuning_type, @tuning_table, @tuning_index, @value, @owner_type, @owner_id)", dynTuning, 100);
            } catch (SqlException)
            {
                return false;
            }
            return true;
        }
    }
}
