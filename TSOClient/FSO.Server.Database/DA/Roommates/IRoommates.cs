using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Roommates
{
    public interface IRoommates
    {
        bool Create(DbRoommate roomie);
        DbRoommate Get(uint avatar_id, int lot_id);
        List<DbRoommate> GetAvatarsLots(uint avatar_id);
        List<DbRoommate> GetLotRoommates(int lot_id);
        uint RemoveRoommate(uint avatar_id, int lot_id);
        bool AcceptRoommateRequest(uint avatar_id, int lot_id);
        bool UpdatePermissionsLevel(uint avatar_id, int lot_id, byte level);
    }
}
