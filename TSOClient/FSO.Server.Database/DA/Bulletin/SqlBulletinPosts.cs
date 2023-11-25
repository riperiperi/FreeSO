using System.Collections.Generic;
using System.Linq;
using Dapper;

namespace FSO.Server.Database.DA.Bulletin
{
    public class SqlBulletinPosts : AbstractSqlDA, IBulletinPosts
    {
        public SqlBulletinPosts(ISqlContext context) : base(context)
        {
        }

        public uint Create(DbBulletinPost bulletin)
        {
            return Context.Connection.Query<uint>("INSERT INTO fso_bulletin_posts (neighborhood_id, avatar_id, title, body, date, flags, lot_id, type) " +
                                        " VALUES (@neighborhood_id, @avatar_id, @title, @body, @date, @flags, @lot_id, @string_type); SELECT LAST_INSERT_ID();"
                                        , bulletin).First();
        }

        public bool Delete(uint bulletin_id)
        {
            return Context.Connection.Execute("DELETE FROM fso_bulletin_posts WHERE bulletin_id = @bulletin_id", new { bulletin_id }) > 0;
        }

        public bool SoftDelete(uint bulletin_id)
        {
            return Context.Connection.Execute("UPDATE fso_bulletin_posts SET deleted = 1 WHERE bulletin_id = @bulletin_id",
                new { bulletin_id }) > 0;
        }

        public bool SetTypeFlag(uint bulletin_id, DbBulletinType type, int flags)
        {
            return Context.Connection.Execute("UPDATE fso_bulletin_posts SET flags = @flags, type = @type WHERE bulletin_id = @bulletin_id", 
                new { bulletin_id, type = type.ToString(), flags }) > 0;
        }

        public List<DbBulletinPost> GetByNhoodId(uint neighborhood_id, uint timeAfter)
        {
            return Context.Connection.Query<DbBulletinPost>("SELECT * FROM fso_bulletin_posts WHERE deleted = 0 AND neighborhood_id = @neighborhood_id AND date > @timeAfter", 
                new { neighborhood_id, timeAfter }).ToList();
        }

        public int CountPosts(uint neighborhood_id, uint timeAfter)
        {
            return Context.Connection.Query<int>("SELECT count(*) FROM fso_bulletin_posts WHERE neighborhood_id = @neighborhood_id AND deleted = 0 AND date > @timeAfter",
                new { neighborhood_id, timeAfter }).FirstOrDefault();
        }

        public uint LastPostID(uint neighborhood_id)
        {
            return Context.Connection.Query<uint>("SELECT bulletin_id FROM fso_bulletin_posts WHERE deleted = 0 AND neighborhood_id = @neighborhood_id ORDER BY bulletin_id DESC LIMIT 1",
                new { neighborhood_id }).FirstOrDefault();
        }

        public DbBulletinPost Get(uint bulletin_id)
        {
            return Context.Connection.Query<DbBulletinPost>("SELECT * FROM fso_bulletin_posts WHERE bulletin_id = @bulletin_id",
                new { bulletin_id }).FirstOrDefault();
        }

        public DbBulletinPost LastUserPost(uint user_id, uint neighborhood_id)
        {
            return Context.Connection.Query<DbBulletinPost>("SELECT b.* FROM fso_bulletin_posts b " +
                "JOIN fso_avatars a ON b.avatar_id = a.avatar_id WHERE a.user_id = @user_id AND b.neighborhood_id = @neighborhood_id " +
                "ORDER BY b.date DESC " +
                "LIMIT 1",
                new { user_id, neighborhood_id }).FirstOrDefault();
        }
    }
}
