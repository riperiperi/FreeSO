using System.Collections.Generic;

namespace FSO.Server.Database.DA.Neighborhoods
{
    public interface INeighborhoods
    {
        List<DbNeighborhood> All(int shard_id);
        DbNeighborhood Get(uint neighborhood_id);
        DbNeighborhood GetByMayor(uint mayor_id);
        DbNeighborhood GetByLocation(uint location);
        int DeleteMissing(int shard_id, List<string> AllGUIDs);
        int UpdateFromJSON(DbNeighborhood update);
        int AddNhood(DbNeighborhood update);
        void UpdateDescription(uint neighborhood_id, string description);
        void UpdateMayor(uint neigh_id, uint? mayor_id);
        void UpdateTownHall(uint neigh_id, uint? lot_id);
        void UpdateCycle(uint neigh_id, uint? cycle_id);
        void UpdateName(uint neighborhood_id, string name);
        void UpdateFlag(uint neighborhood_id, uint flag);

        DbNhoodBan GetNhoodBan(uint user_id);
        bool AddNhoodBan(DbNhoodBan ban);

        List<DbNeighborhood> SearchExact(int shard_id, string name, int limit);
        List<DbNeighborhood> SearchWildcard(int shard_id, string name, int limit);
    }
}
