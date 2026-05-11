namespace FSO.Server.Database.DA.ActionLog
{
    // Stable byte enum matching fso_action_log.action_type. Add new types
    // here AND in the comment on the column in 0034_daily_quests.sql.
    // Never renumber existing values — old rows have these baked in.
    public static class ActionType
    {
        public const byte MoneyEarned   = 1;
        public const byte SkillGained   = 2;
        public const byte LotVisited    = 3;
        public const byte CatalogBought = 4;
    }
}