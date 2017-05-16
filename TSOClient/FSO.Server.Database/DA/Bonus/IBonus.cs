using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Bonus
{
    public interface IBonus
    {
        IEnumerable<DbBonus> GetByAvatarId(uint avatar_id);
        IEnumerable<DbBonusMetrics> GetMetrics(DateTime date, int shard_id);
        void Insert(IEnumerable<DbBonus> bonus);
        void Purge(DateTime date);
    }
}
