using FSO.Server.Database.DA.Tuning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.DynPayouts
{
    public interface IDynPayouts
    {
        List<DbTransSummary> GetSummary(int limitDay);
        bool InsertDynRecord(List<DbDynPayout> dynPayout);
        bool ReplaceDynTuning(List<DbTuning> dynTuning);

        List<DbDynPayout> GetPayoutHistory(int limitDay);
        bool Purge(int limitDay);
    }
}
