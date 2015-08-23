using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Avatars
{
    public interface IAvatars
    {
        void Create(DbAvatar avatar);

        DbAvatar Get(uint id);
        IEnumerable<DbAvatar> All(int shard_id);
        List<DbAvatar> GetByUserId(uint user_id);


        List<DbAvatar> SearchExact(string name, int limit);
        List<DbAvatar> SearchWildcard(string name, int limit);
    }
}
