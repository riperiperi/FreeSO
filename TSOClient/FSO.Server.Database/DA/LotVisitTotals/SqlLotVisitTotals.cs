using FSO.Server.Database.DA.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                int y = 22;
            }
        }
    }
}
