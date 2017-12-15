using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Transactions
{
    public interface ITransactions
    {
        void Purge(int day);
    }
}
