using FSO.Server.Database.DA.Utils;
using System;
using System.Collections.Generic;

namespace FSO.Server.Database.DA.Avatars
{
    public interface IAvatars
    {
        uint Create(DbAvatar avatar);

        DbAvatar Get(uint id);
        List<DbAvatar> GetMultiple(uint[] id);
        bool Delete(uint id);
        int GetPrivacyMode(uint id);
        int GetModerationLevel(uint id);
        DbJobLevel GetCurrentJobLevel(uint avatar_id);
        List<DbJobLevel> GetJobLevels(uint avatar_id);
        IEnumerable<DbAvatar> All();
        IEnumerable<DbAvatar> All(int shard_id);
        PagedList<DbAvatar> AllByPage(int shard_id, int offset, int limit, string orderBy);
        // Number of distinct user accounts that have at least one avatar
        // on this shard. Used by the starter-budget cap so it counts
        // unique players, not avatars (one user can have multiple sims
        // and we only want to cap how many users got bootstrapped).
        int CountUniqueUsersOnShard(int shard_id);
        List<uint> GetLivingInNhood(uint nhood_id);
        List<AvatarRating> GetPossibleCandidatesNhood(uint nhood_id);

        List<DbAvatar> GetByUserId(uint user_id);
        List<DbAvatarSummary> GetSummaryByUserId(uint user_id);

        int GetOtherLocks(uint avatar_id, string except);

        DbSkillLockBonusPurchase PurchaseSkillLockBonus(uint avatar_id, uint target_bonus, int cost);

        int GetBudget(uint avatar_id);
        // Unconditional system credit — adds amount to the avatar's
        // budget. Used by milestone tasks (BirthdayGiftTask) and admin
        // tools that don't have a debit-side account. Returns rows
        // affected (1 on success, 0 if avatar_id missing). For
        // peer-to-peer transfers use Transaction() instead.
        int CreditBudget(uint avatar_id, int amount);
        DbTransactionResult Transaction(uint source_id, uint avatar_id, int amount, short reason);
        DbTransactionResult Transaction(uint source_id, uint avatar_id, int amount, short reason, Func<bool> transactionInject);
        DbTransactionResult TestTransaction(uint source_id, uint avatar_id, int amount, short reason);

        void UpdateDescription(uint id, string description);
        void UpdatePrivacyMode(uint id, byte privacy);
        void UpdateAvatarLotSave(uint id, DbAvatar avatar);
        void UpdateAvatarJobLevel(DbJobLevel jobLevel);
        void UpdateMoveDate(uint id, uint date);
        void UpdateMayorNhood(uint id, uint? nhood);

        List<DbAvatar> SearchExact(int shard_id, string name, int limit);
        List<DbAvatar> SearchWildcard(int shard_id, string name, int limit);
    }

    public class AvatarRating
    {
        public uint avatar_id { get; set; }
        public string name { get; set; }
        public float? rating { get; set; }
    }
}
