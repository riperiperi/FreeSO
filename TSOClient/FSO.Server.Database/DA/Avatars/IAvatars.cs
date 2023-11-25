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
        List<uint> GetLivingInNhood(uint nhood_id);
        List<AvatarRating> GetPossibleCandidatesNhood(uint nhood_id);

        List<DbAvatar> GetByUserId(uint user_id);
        List<DbAvatarSummary> GetSummaryByUserId(uint user_id);

        int GetOtherLocks(uint avatar_id, string except);

        int GetBudget(uint avatar_id);
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
