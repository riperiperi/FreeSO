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
    }
}