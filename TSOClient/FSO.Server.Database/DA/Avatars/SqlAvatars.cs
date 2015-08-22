using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Avatars
{
    public class SqlAvatars : AbstractSqlDA, IAvatars
    {
        public SqlAvatars(ISqlContext context) : base(context){
        }

        public IEnumerable<DbAvatar> All(int shard_id){
            return Context.Connection.Query<DbAvatar>("SELECT * FROM fso_avatars WHERE shard_id = @shard_id", new { shard_id = shard_id });
        }

        public DbAvatar Get(uint id){
            return Context.Connection.Query<DbAvatar>("SELECT * FROM fso_avatars WHERE avatar_id = @id", new { id = id }).FirstOrDefault();
        }

        public void Create(DbAvatar avatar)
        {
            Context.Connection.Execute("INSERT INTO fso_avatars (shard_id, user_id, name, " + 
                                        "gender, date, skin_tone, head, body, description) " + 
                                        " VALUES (@shard_id, @user_id, @name, @gender, @date, " + 
                                        " @skin_tone, @head, @body, @description)", avatar);
        }


        public List<DbAvatar> GetByUserId(uint user_id)
        {
            return Context.Connection.Query<DbAvatar>(
                "SELECT * FROM fso_avatars WHERE user_id = @user_id", 
                new { user_id = user_id }
            ).ToList();
        }
    }
}
