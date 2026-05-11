using System.Collections.Generic;

namespace FSO.Server.Database.DA.DailyQuests
{
    public interface IDailyQuests
    {
        // Today's three quests for an avatar (0–3 rows).
        IEnumerable<DbDailyQuest> GetForDay(uint avatar_id, uint day);

        // Inserted by RollDailyQuestsTask.
        void Insert(DbDailyQuest quest);

        // Increment progress in-place for every un-completed quest of this
        // type for (avatar_id, day). Caps at target and sets completed_ts
        // for any quest that crosses the threshold. Returns rows affected.
        int IncrementProgress(uint avatar_id, uint day, byte quest_type, ulong delta);

        // Completed-but-not-yet-paid quests for the given day. Used by the
        // payout pass in RollDailyQuestsTask.
        IEnumerable<DbDailyQuest> GetUnpaidForDay(uint day);

        // Stamp paid_ts. Idempotent.
        void MarkPaid(uint avatar_id, uint day, byte slot, uint ts);

        // Avatars whose owning user logged in since `activityCutoffEpoch` AND
        // who have no daily-quest rows yet for `day`. Used by
        // RollDailyQuestsTask to pick targets. LEFT JOIN guard makes the
        // task safe to re-run (e.g. cron caught up after a missed slot)
        // without double-inserting.
        IEnumerable<DbRollableAvatar> GetAvatarsNeedingRoll(uint day, uint activityCutoffEpoch);
    }

    // Light projection — just the fields the roll task needs from
    // fso_avatars. Field names match the SELECT aliases.
    public class DbRollableAvatar
    {
        public uint avatar_id { get; set; }
        public int shard_id { get; set; }
        public uint created_epoch { get; set; }
        public string name { get; set; }
    }
}