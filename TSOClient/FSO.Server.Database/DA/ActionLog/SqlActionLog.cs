using System;
using Dapper;
using FSO.Server.Database.DA.Utils;

namespace FSO.Server.Database.DA.ActionLog
{
    public class SqlActionLog : AbstractSqlDA, IActionLog
    {
        public SqlActionLog(ISqlContext context) : base(context) { }

        public void Insert(DbAction action)
        {
            Context.Connection.Execute(
                @"INSERT INTO fso_action_log
                    (avatar_id, day, action_type, value, parameter, ts)
                  VALUES (@avatar_id, @day, @action_type, @value, @parameter, @ts)",
                action);
        }

        public bool ExistsToday(uint avatar_id, uint day, byte action_type, uint parameter)
        {
            // Indexed point lookup via idx_avatar_day_type — sub-ms.
            var any = Context.Connection.ExecuteScalar<long?>(
                @"SELECT 1 FROM fso_action_log
                   WHERE avatar_id = @avatar_id
                     AND day = @day
                     AND action_type = @action_type
                     AND parameter = @parameter
                   LIMIT 1",
                new { avatar_id, day, action_type, parameter });
            return any.HasValue;
        }

        public int Purge(uint olderThanDay)
        {
            return Context.Connection.Execute(
                "DELETE FROM fso_action_log WHERE day < @olderThanDay",
                new { olderThanDay });
        }

        public void RecordAction(uint avatar_id, byte action_type, ulong value, uint? parameter)
        {
            if (value == 0) return;

            uint now = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            uint today = now / 86400;

            // 1) audit log row
            Context.Connection.Execute(
                @"INSERT INTO fso_action_log
                    (avatar_id, day, action_type, value, parameter, ts)
                  VALUES (@avatar_id, @day, @action_type, @value, @parameter, @ts)",
                new { avatar_id, day = today, action_type, value, parameter, ts = now });

            // 2) in-place quest progress update. quest_type=action_type in v1.
            //    LEAST() caps progress at target; CASE stamps completed_ts on
            //    the row that crosses the threshold this tick.
            Context.Connection.Execute(
                @"UPDATE fso_daily_quests
                     SET progress = LEAST(target, progress + @delta),
                         completed_ts = CASE
                             WHEN progress + @delta >= target THEN @ts
                             ELSE completed_ts
                         END
                   WHERE avatar_id = @avatar_id
                     AND day = @day
                     AND quest_type = @quest_type
                     AND completed_ts IS NULL",
                new { avatar_id, day = today, quest_type = action_type, delta = value, ts = now });
        }

        public bool RecordActionIdempotent(uint avatar_id, byte action_type, ulong value, uint parameter)
        {
            uint now = (uint)DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            uint today = now / 86400;

            // Skip duplicates entirely — no log entry, no progress bump.
            // Re-visiting a lot multiple times in one day shouldn't appear
            // in the audit trail twice or count twice toward the unique
            // visit quest.
            if (ExistsToday(avatar_id, today, action_type, parameter)) return false;

            Context.Connection.Execute(
                @"INSERT INTO fso_action_log
                    (avatar_id, day, action_type, value, parameter, ts)
                  VALUES (@avatar_id, @day, @action_type, @value, @parameter, @ts)",
                new { avatar_id, day = today, action_type, value, parameter, ts = now });

            Context.Connection.Execute(
                @"UPDATE fso_daily_quests
                     SET progress = LEAST(target, progress + @delta),
                         completed_ts = CASE
                             WHEN progress + @delta >= target THEN @ts
                             ELSE completed_ts
                         END
                   WHERE avatar_id = @avatar_id
                     AND day = @day
                     AND quest_type = @quest_type
                     AND completed_ts IS NULL",
                new { avatar_id, day = today, quest_type = action_type, delta = value, ts = now });

            return true;
        }
    }
}