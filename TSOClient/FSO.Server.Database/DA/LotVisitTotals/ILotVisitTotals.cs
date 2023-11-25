using System;
using System.Collections.Generic;

namespace FSO.Server.Database.DA.LotVisitTotals
{
    public interface ILotVisitTotals
    {
        void Insert(IEnumerable<DbLotVisitTotal> input);
        void Purge(DateTime date);
    }
}
