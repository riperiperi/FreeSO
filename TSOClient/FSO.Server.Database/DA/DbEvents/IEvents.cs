using FSO.Server.Database.DA.Utils;
using System;
using System.Collections.Generic;

namespace FSO.Server.Database.DA.DbEvents
{
    public interface IEvents
    {
        PagedList<DbEvent> All(int offset = 0, int limit = 20, string orderBy = "start_day");
        List<DbEvent> GetActive(DateTime time);
        int Add(DbEvent evt);
        bool Delete(int event_id);

        bool TryParticipate(DbEventParticipation p);
        bool Participated(DbEventParticipation p);
        List<uint> GetParticipatingUsers(int event_id);

        bool GenericAvaTryParticipate(DbGenericAvatarParticipation p);
        bool GenericAvaParticipated(DbGenericAvatarParticipation p);
        List<uint> GetGenericParticipatingAvatars(string genericName);

        List<DbEvent> GetLatestNameDesc(int limit);
    }
}
