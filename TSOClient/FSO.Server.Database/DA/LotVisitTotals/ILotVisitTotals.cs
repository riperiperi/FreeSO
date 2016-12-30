using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.LotVisitTotals
{
    public interface ILotVisitTotals
    {
        void Insert(IEnumerable<DbLotVisitTotal> input);
        void Purge(DateTime date);
    }
}
