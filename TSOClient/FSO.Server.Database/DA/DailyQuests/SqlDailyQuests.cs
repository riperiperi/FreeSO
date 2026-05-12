using System.Collections.Generic;
using Dapper;
using FSO.Server.Database.DA.Utils;

namespace FSO.Server.Database.DA.DailyQuests
{
    public class SqlDailyQuests : AbstractSqlDA, IDailyQuests
    {
        public SqlDailyQuests(ISqlContext context) : base(context) { }

        public IEnumerable<DbDailyQuest> GetForDay(uint avatar_id, uint day)
        {
            return Context.Connection.Query<DbDailyQuest>(
                @"SELECT * FROM fso_daily_quests
                   WHERE avatar_id = @avatar_id AND day = @day
                   ORDER BY slot",
                new { avatar_id, day });
        }

        public void Insert(DbDailyQuest quest)
        {
            Context.Connection.Execute(
                @"INSERT INTO fso_daily_quests
                    (avatar_id, day, slot, quest_type, target, progress,
                     reward, parameter, completed_ts, paid_ts)
                  VALUES (@avatar_id, @day, @slot, @quest_type, @target, @progress,
                          @reward, @parameter, @completed_ts, @paid_ts)",
                quest);
        }

        public int IncrementProgress(uint avatar_id, uint day, byte quest_type, ulong delta)
        {
            // Cap at target with LEAST(); stamp completed_ts on the row that
            // crosses the threshold. completed_ts IS NULL filter is the
            // load-bearing one — a completed quest is immutable.
            return Context.Connection.Execute(
                @"UPDATE fso_daily_quests
                     SET progress = LEAST(target, progress + @delta),
                         completed_ts = CASE
                             WHEN progress + @delta >= target THEN UNIX_TIMESTAMP()
                             ELSE completed_ts
                         END
                   WHERE avatar_id = @avatar_id
                     AND day = @day
                     AND quest_type = @quest_type
                     AND completed_ts IS NULL",
                new { avatar_id, day, quest_type, delta });
        }

        public IEnumerable<DbDailyQuest> GetUnpaidForDay(uint day)
        {
            return Context.Connection.Query<DbDailyQuest>(
                @"SELECT * FROM fso_daily_quests
                   WHERE day = @day
                     AND completed_ts IS NOT NULL
                     AND paid_ts IS NULL",
                new { day });
        }

        public int MarkPaid(uint avatar_id, uint day, byte slot, uint ts)
        {
            // The WHERE paid_ts IS NULL clause is the race guard: only the
            // first concurrent caller's UPDATE matches. ExecuteScalar with
            // ROW_COUNT() is the conventional MariaDB pattern, but Dapper's
            // Execute returns the affected-row count directly which is
            // simpler.
            return Context.Connection.Execute(
                @"UPDATE fso_daily_quests
                     SET paid_ts = @ts
                   WHERE avatar_id = @avatar_id AND day = @day AND slot = @slot
                     AND paid_ts IS NULL",
                new { avatar_id, day, slot, ts });
        }

        public IEnumerable<DbRollableAvatar> GetAvatarsNeedingRoll(uint day, uint activityCutoffEpoch)
        {
            return Context.Connection.Query<DbRollableAvatar>(
                @"SELECT a.avatar_id          AS avatar_id,
                         a.shard_id           AS shard_id,
                         a.date               AS created_epoch,
                         a.name               AS name
                    FROM fso_avatars a
                    JOIN fso_users u ON u.user_id = a.user_id
                    LEFT JOIN fso_daily_quests q
                      ON q.avatar_id = a.avatar_id AND q.day = @day
                   WHERE u.last_login >= @cutoff
                     AND q.avatar_id IS NULL
                   GROUP BY a.avatar_id, a.shard_id, a.date, a.name",
                new { day, cutoff = activityCutoffEpoch });
        }
    }
}