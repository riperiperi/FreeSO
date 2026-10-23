using FSO.Common.Enum;

namespace FSO.Server.Database.DA.ArchiveTop100
{
    public interface IArchiveTop100
    {
        List<DbArchiveTop100WithLot> All(int shard_id);
        List<DbArchiveTop100WithLot> GetTop100(int shard_id, LotCategory category);
        List<DbArchiveTop100WithLot> GetNeighborhoodCategoryBests(int shard_id);
        List<DbArchiveTop100WithLot> GetNeighborhoodBests(int shard_id);
        int Create(DbArchiveTop100 entry);
        bool Clear(int shard_id);
        int CreateAvatar(DbArchiveTop100Avatar entry);
        List<DbArchiveTop100Avatar> AllAvatars(int shard_id);
    }
}
