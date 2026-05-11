namespace FSO.Server.Database.DA.DailyQuests
{
    // Stable byte enum matching fso_daily_quests.quest_type. v1 maps 1:1 to
    // ActionType but is kept separate so we can later add composite quests
    // (e.g. EARN_FROM_COOKING = MoneyEarned filtered by parameter).
    public static class QuestType
    {
        public const byte Earn  = 1;  // MoneyEarned aggregation
        public const byte Skill = 2;  // SkillGained aggregation (value is hundredths of a point)
        public const byte Visit = 3;  // unique LotVisited count
        public const byte Buy   = 4;  // CatalogBought aggregation
    }
}