using Dapper;

namespace FSO.Server.Database.DA.ArchiveRecents
{
    internal class SqlArchiveRecents : AbstractSqlDA, IArchiveRecents
    {
        public SqlArchiveRecents(ISqlContext context) : base(context)
        {
        }

        public IEnumerable<int> AvatarsByUser(int user_id, int limit)
        {
            return Context.Connection.Query<int>(
                Context.CompatLayer("SELECT avatar_id from fso_archive_recents " +
                "WHERE user_id = @user_id " +
                "ORDER BY last_timestamp DESC " +
                "LIMIT @limit"),
                new { user_id, limit });
        }

        public void RecordAvatarUse(int user_id, int avatar_id)
        {
            var use = new DbArchiveRecent
            {
                user_id = user_id,
                avatar_id = avatar_id,
                last_timestamp = DateTime.UtcNow,
            };

            Context.Connection.Execute(Context.CompatLayer("INSERT INTO fso_archive_recents (user_id, avatar_id, last_timestamp) " +
                "VALUES(@user_id, @avatar_id, @last_timestamp) " +
                "ON DUPLICATE KEY UPDATE last_timestamp = @last_timestamp; ", "`user_id`,`avatar_id`"), use);
        }
    }
}
