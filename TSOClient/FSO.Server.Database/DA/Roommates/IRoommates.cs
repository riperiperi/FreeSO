using System.Collections.Generic;

namespace FSO.Server.Database.DA.Roommates
{
    public interface IRoommates
    {
        bool Create(DbRoommate roomie);
        bool CreateOrUpdate(DbRoommate roomie);
        DbRoommate Get(uint avatar_id, int lot_id);
        List<DbRoommate> GetAvatarsLots(uint avatar_id);
        List<DbRoommate> GetLotRoommates(int lot_id);
        uint RemoveRoommate(uint avatar_id, int lot_id);
        bool DeclineRoommateRequest(uint avatar_id, int lot_id);
        bool AcceptRoommateRequest(uint avatar_id, int lot_id);
        bool UpdatePermissionsLevel(uint avatar_id, int lot_id, byte level);
    }
}
