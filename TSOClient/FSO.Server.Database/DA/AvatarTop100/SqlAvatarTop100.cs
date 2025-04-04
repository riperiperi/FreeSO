using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Enum;

namespace FSO.Server.Database.DA.AvatarTop100
{
    public class SqlAvatarTop100 : AbstractSqlDA, IAvatarTop100
    {
        public SqlAvatarTop100(ISqlContext context) : base(context)
        {
        }

        public IEnumerable<DbAvatarTop100> All()
        {
            return Context.Connection.Query<DbAvatarTop100>("SELECT top.*, a.name as avatar_name FROM fso_avatar_top_100 top LEFT JOIN fso_avatars a ON top.avatar_id = a.avatar_id");
        }

        public bool Calculate(int shard_id)
        {
            try
            {
                Context.Connection.Execute("CALL fso_avatar_top_100_calc_all(@shard_id);", new { shard_id = shard_id });
                return true;
            }catch(Exception ex)
            {
                return false;
            }
        }
        public IEnumerable<DbAvatarTop100> GetAllByShard(int shard_id)
        {
            return Context.Connection.Query<DbAvatarTop100>("SELECT top.*, a.name as avatar_name FROM fso_avatar_top_100 top LEFT JOIN fso_avatars a ON top.avatar_id = a.avatar_id WHERE top.shard_id = @shard_id", new
            {
                shard_id = shard_id
            });
        }
        public IEnumerable<DbAvatarTop100> GetByCategory(int shard_id, AvatarTop100Category category)
        {
            return Context.Connection.Query<DbAvatarTop100>("SELECT top.*, a.name as avatar_name FROM fso_avatar_top_100 top LEFT JOIN fso_avatars a ON top.avatar_id = a.avatar_id WHERE top.category = @category AND top.shard_id = @shard_id", new
            {
                category = category.ToString(),
                shard_id = shard_id
            });
        }
    }
}
