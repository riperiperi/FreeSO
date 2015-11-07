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

        public uint Create(DbAvatar avatar)
        {
            return (uint)Context.Connection.Query<int>("INSERT INTO fso_avatars (shard_id, user_id, name, " +
                                        "gender, date, skin_tone, head, body, description) " +
                                        " VALUES (@shard_id, @user_id, @name, @gender, @date, " +
                                        " @skin_tone, @head, @body, @description); SELECT LAST_INSERT_ID();", new
                                        {
                                            shard_id = avatar.shard_id,
                                            user_id = avatar.user_id,
                                            name = avatar.name,
                                            gender = avatar.gender.ToString(),
                                            date = avatar.date,
                                            skin_tone = avatar.skin_tone,
                                            head = avatar.head,
                                            body = avatar.body,
                                            description = avatar.description
                                        }).First();
        }


        public List<DbAvatar> GetByUserId(uint user_id)
        {
            return Context.Connection.Query<DbAvatar>(
                "SELECT * FROM fso_avatars WHERE user_id = @user_id", 
                new { user_id = user_id }
            ).ToList();
        }

        public List<DbAvatar> SearchExact(int shard_id, string name, int limit)
        {
            return Context.Connection.Query<DbAvatar>(
                "SELECT avatar_id, name FROM fso_avatars WHERE shard_id = @shard_id AND name = @name LIMIT @limit",
                new { name = name, limit = limit, shard_id = shard_id }
            ).ToList();
        }

        public List<DbAvatar> SearchWildcard(int shard_id, string name, int limit)
        {
            return Context.Connection.Query<DbAvatar>(
                "SELECT avatar_id, name FROM fso_avatars WHERE shard_id = @shard_id AND name LIKE @name LIMIT @limit",
                new { name = name, limit = limit, shard_id = shard_id }
            ).ToList();
        }

        public void UpdateDescription(uint id, string description)
        {
            Context.Connection.Query("UPDATE fso_avatars SET description = @desc WHERE avatar_id = @id", new { id = id, desc = description });
        }
    }
}
