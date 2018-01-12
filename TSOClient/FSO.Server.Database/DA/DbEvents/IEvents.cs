using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.DbEvents
{
    public interface IEvents
    {
        List<DbEvent> GetActive(DateTime time);
        int Add(DbEvent evt);
        bool Delete(int event_id);

        bool TryParticipate(DbEventParticipation p);
        bool Participated(DbEventParticipation p);
        List<uint> GetParticipatingUsers(int event_id);

        List<DbEvent> GetLatestNameDesc(int limit);
    }
}
