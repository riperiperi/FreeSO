using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Objects
{
    public interface IObjects
    {
        uint Create(DbObject obj);

        DbObject Get(uint id);
        bool Delete(uint id);
        IEnumerable<DbObject> All(int shard_id);
        List<DbObject> GetAvatarInventory(uint avatar_id);
        List<DbObject> ObjOfTypeInAvatarInventory(uint avatar_id, uint guid);
        List<DbObject> GetObjectOwners(IEnumerable<uint> object_ids);
        int ReturnLostObjects(uint lot_id, IEnumerable<uint> object_ids);
        bool ConsumeObjsOfTypeInAvatarInventory(uint avatar_id, uint guid, int num);
        List<DbObject> GetByAvatarId(uint avatar_id);

        bool UpdatePersistState(uint id, DbObject obj);
        bool SetInLot(uint id, uint? lot_id);
    }
}
