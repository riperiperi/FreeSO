namespace FSO.Server.Database.DA.ActionLog
{
    public interface IActionLog
    {
        // Append a row. Caller fills in the ts/day fields.
        void Insert(DbAction action);

        // Has this avatar already logged an action of (type, parameter) today?
        // Used for LOT_VISITED idempotency — first visit of the day counts;
        // subsequent visits don't bump quest progress.
        bool ExistsToday(uint avatar_id, uint day, byte action_type, uint parameter);

        // Drop rows with day < olderThanDay. Returns rows removed.
        int Purge(uint olderThanDay);
    }
}