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

        public void MarkPaid(uint avatar_id, uint day, byte slot, uint ts)
        {
            Context.Connection.Execute(
                @"UPDATE fso_daily_quests
                     SET paid_ts = @ts
                   WHERE avatar_id = @avatar_id AND day = @day AND slot = @slot
                     AND paid_ts IS NULL",
                new { avatar_id, day, slot, ts });
        }
    }
}