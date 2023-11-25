using Dapper;
using FSO.Server.Database.DA.Utils;
using System;
using System.Collections.Generic;

namespace FSO.Server.Database.DA.LotVisitTotals
{
    public class SqlLotVisitTotals : AbstractSqlDA, ILotVisitTotals
    {
        public SqlLotVisitTotals(ISqlContext context) : base(context)
        {
        }

        public void Insert(IEnumerable<DbLotVisitTotal> input)
        {
            try {
                Context.Connection.ExecuteBufferedInsert("INSERT INTO fso_lot_visit_totals (lot_id, date, minutes) VALUES (@lot_id, @date, @minutes) ON DUPLICATE KEY UPDATE minutes=VALUES(minutes)", input, 100);
            }catch(Exception ex)
            {
            }
        }

        public void Purge(DateTime date)
        {
            Context.Connection.Execute("DELETE FROM fso_lot_visit_totals WHERE date < @date", new { date = date });
        }
    }
}
