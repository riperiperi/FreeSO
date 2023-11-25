using FSO.Common.Enum;
using FSO.Server.Database.DA.Utils;
using System.Collections.Generic;

namespace FSO.Server.Database.DA.Lots
{
    public interface ILots
    {
        IEnumerable<DbLot> All(int shard_id);
        PagedList<DbLot> AllByPage(int shard_id, int offset, int limit, string orderBy);
        List<uint> GetLocationsInNhood(uint nhood_id);
        List<uint> GetCommunityLocations(int shard_id);
        List<DbLot> AllLocations(int shard_id);
        DbLot GetByName(int shard_id, string name);
        DbLot GetByLocation(int shard_id, uint location);
        List<DbLot> GetAdjToLocation(int shard_id, uint location);
        DbLot GetByOwner(uint owner_id);
        DbLot Get(int id);
        List<DbLot> GetMultiple(int[] ids);
        List<DbLot> Get(IEnumerable<int> ids);
        uint Create(DbLot lot);
        bool Delete(int id);

        void RenameLot(int id, string newName);
        void SetDirty(int id, byte dirty);
        DbLot Get3DWork();

        List<DbLot> SearchExact(int shard_id, string name, int limit);
        List<DbLot> SearchWildcard(int shard_id, string name, int limit);

        void UpdateRingBackup(int lot_id, sbyte ring_backup_num);
        void UpdateDescription(int lot_id, string description);
        void UpdateLotCategory(int lot_id, LotCategory category, uint skillMode);
        void UpdateLotSkillMode(int lot_id, uint skillMode);
        void UpdateLotAdmitMode(int lot_id, byte admit_mode);
        bool UpdateLocation(int lot_id, uint location, bool startFresh);
        void UpdateOwner(int lot_id, uint? avatar_id);
        void ReassignOwner(int lot_id);

        void CreateLotServerTicket(DbLotServerTicket ticket);
        void DeleteLotServerTicket(string id);
        DbLotServerTicket GetLotServerTicket(string id);
        List<DbLotServerTicket> GetLotServerTicketsForClaimedAvatar(int claim_id);

        int UpdateAllNeighborhoods(int shard_id);
    }
}
