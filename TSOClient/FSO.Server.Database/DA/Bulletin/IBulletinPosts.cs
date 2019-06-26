using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Bulletin
{
    public interface IBulletinPosts
    {
        DbBulletinPost Get(uint bulletin_id);
        int CountPosts(uint neighborhood_id, uint timeAfter);
        uint LastPostID(uint neighborhood_id);
        DbBulletinPost LastUserPost(uint user_id, uint neighborhood_id);
        List<DbBulletinPost> GetByNhoodId(uint neighborhood_id, uint timeAfter);
        uint Create(DbBulletinPost bulletin);
        bool Delete(uint bulletin_id);
        bool SoftDelete(uint bulletin_id);
        bool SetTypeFlag(uint bulletin_id, DbBulletinType type, int flag);
    }
}
