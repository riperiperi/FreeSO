using FSO.Common.Enum;
using System;
using System.Collections.Generic;

namespace FSO.Server.Database.DA.LotTop100
{
    public interface ILotTop100
    {
        void Replace(IEnumerable<DbLotTop100> values);
        IEnumerable<DbLotTop100> All();
        IEnumerable<DbLotTop100> GetAllByShard(int shard_id);
        IEnumerable<DbLotTop100> GetByCategory(int shard_id, LotCategory category);
        bool Calculate(DateTime date, int shard_id);
    }
}
