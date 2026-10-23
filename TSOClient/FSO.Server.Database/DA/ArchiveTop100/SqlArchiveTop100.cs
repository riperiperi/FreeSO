using Dapper;
using FSO.Common.Enum;

namespace FSO.Server.Database.DA.ArchiveTop100
{
    public class SqlArchiveTop100 : AbstractSqlDA, IArchiveTop100
    {
        public SqlArchiveTop100(ISqlContext context) : base(context)
        {
        }

        public bool Clear(int shard_id)
        {
            return Context.Connection.Execute("DELETE FROM fso_archive_top100 WHERE shard_id = @shard_id", new { shard_id }) > 0 ||
                Context.Connection.Execute("DELETE FROM fso_archive_top100_avatar WHERE shard_id = @shard_id", new { shard_id }) > 0;
        }

        public int Create(DbArchiveTop100 entry)
        {
            var result = Context.Connection.Query<int>(Context.CompatLayer("INSERT INTO fso_archive_top100 (shard_id, lot_id, minutes, category, rank)" +
                                    " VALUES (@shard_id, @lot_id, @minutes, @category, @rank);" +
                                    " SELECT LAST_INSERT_ID();"), entry).First();
            return result;
        }

        public List<DbArchiveTop100WithLot> GetNeighborhoodBests(int shard_id)
        {
            return [
                ..Context.Connection.Query<DbArchiveTop100WithLot>(
                    "SELECT a.*, b.neighborhood_id, b.location, b.name, max(a.minutes) FROM fso_archive_top100 a INNER JOIN fso_lots b on a.lot_id = b.lot_id WHERE a.shard_id = @shard_id GROUP BY b.neighborhood_id",
                    new { shard_id }
                    )
                ];
        }

        public List<DbArchiveTop100WithLot> GetNeighborhoodCategoryBests(int shard_id)
        {
            return [
                ..Context.Connection.Query<DbArchiveTop100WithLot>(
                    "SELECT a.*, b.neighborhood_id, b.location, b.name, max(a.minutes) FROM fso_archive_top100 a INNER JOIN fso_lots b on a.lot_id = b.lot_id WHERE a.shard_id = @shard_id GROUP BY a.category, b.neighborhood_id",
                    new { shard_id }
                    )
                ];
        }

        public List<DbArchiveTop100WithLot> GetTop100(int shard_id, LotCategory category)
        {
            return [
                ..Context.Connection.Query<DbArchiveTop100WithLot>(
                    "SELECT a.*, b.neighborhood_id, b.location, b.name FROM fso_archive_top100 a INNER JOIN fso_lots b on a.lot_id = b.lot_id WHERE a.shard_id = @shard_id AND a.category = @category ORDER BY rank ASC LIMIT 100",
                    new { shard_id, category = (int)category }
                    )
                ];
        }

        public List<DbArchiveTop100WithLot> All(int shard_id)
        {
            return [
                .. Context.Connection.Query<DbArchiveTop100WithLot>(
                    "SELECT a.*, b.neighborhood_id, b.location, b.name FROM fso_archive_top100 a INNER JOIN fso_lots b on a.lot_id = b.lot_id WHERE a.shard_id = @shard_id ORDER BY a.minutes DESC",
                    new { shard_id })
                ];
        }

        public int CreateAvatar(DbArchiveTop100Avatar entry)
        {
            var result = Context.Connection.Query<int>(Context.CompatLayer("INSERT INTO fso_archive_top100_avatar (shard_id, avatar_id, minutes)" +
                                    " VALUES (@shard_id, @avatar_id, @minutes);" +
                                    " SELECT LAST_INSERT_ID();"), entry).First();
            return result;
        }

        public List<DbArchiveTop100Avatar> AllAvatars(int shard_id)
        {
            return [
                .. Context.Connection.Query<DbArchiveTop100Avatar>(
                    "SELECT * FROM fso_archive_top100_avatar WHERE shard_id = @shard_id ORDER BY minutes DESC",
                    new { shard_id })
                ];
        }
    }
}
