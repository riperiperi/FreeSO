using FSO.Common.Enum;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.LotTop100
{
    public interface IAvatarTop100
    {
        IEnumerable<DbAvatarTop100> All();
        IEnumerable<DbAvatarTop100> GetAllByShard(int shard_id);
        IEnumerable<DbAvatarTop100> GetByCategory(int shard_id, AvatarTop100Category category);
        bool Calculate(int shard_id);
    }
}
