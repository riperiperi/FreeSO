using FSO.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.LotTop100
{
    public interface ILotTop100
    {
        void Replace(IEnumerable<DbLotTop100> values);
        IEnumerable<DbLotTop100> All();
        IEnumerable<DbLotTop100> GetByCategory(int shard_id, LotCategory category);
    }
}
