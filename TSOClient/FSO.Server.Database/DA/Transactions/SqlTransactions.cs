using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Transactions
{
    public class SqlTransactions : AbstractSqlDA, ITransactions
    {
        public SqlTransactions(ISqlContext context) : base(context)
        {
        }

        public void Purge(int day)
        {
            Context.Connection.Query("DELETE FROM fso_transactions WHERE day < @day", new { day = day });
        }
    }
}
