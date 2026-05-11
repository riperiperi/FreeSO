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

        // High-level helpers used by event-hook sites. Both compute (today, ts)
        // internally so callers don't need to. quest_type maps 1:1 to
        // action_type in v1.

        // Always logs + always bumps. Use for unbounded-progress actions like
        // MONEY_EARNED, SKILL_GAINED, CATALOG_BOUGHT where every event counts.
        void RecordAction(uint avatar_id, byte action_type, ulong value, uint? parameter);

        // Logs + bumps only if (avatar, today, action_type, parameter) doesn't
        // already exist. Use for unique-count actions like LOT_VISITED where
        // re-visiting the same lot shouldn't double-count. Returns true if
        // the action was new (and therefore recorded), false if duplicate.
        bool RecordActionIdempotent(uint avatar_id, byte action_type, ulong value, uint parameter);
    }
}