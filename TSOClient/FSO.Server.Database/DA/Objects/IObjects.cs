using System.Collections.Generic;

namespace FSO.Server.Database.DA.Objects
{
    public interface IObjects
    {
        uint Create(DbObject obj);

        DbObject Get(uint id);
        bool Delete(uint id);
        IEnumerable<DbObject> All(int shard_id);
        List<DbObject> GetAvatarInventory(uint avatar_id);
        List<DbObject> GetAvatarInventoryWithAttrs(uint avatar_id);
        List<DbObject> ObjOfTypeForAvatar(uint avatar_id, uint guid);
        List<DbObject> ObjOfTypeInAvatarInventory(uint avatar_id, uint guid);
        List<DbObject> GetObjectOwners(IEnumerable<uint> object_ids);
        int ReturnLostObjects(uint lot_id, IEnumerable<uint> object_ids);
        bool ConsumeObjsOfTypeInAvatarInventory(uint avatar_id, uint guid, int num);
        List<DbObject> GetByAvatarId(uint avatar_id);
        List<DbObject> GetByAvatarIdLot(uint avatar_id, uint lot_id);
        int UpdateObjectOwnerLot(uint avatar_id, int lot_id, uint targ_avatar_id, List<uint> untradableGUIDs);

        bool UpdatePersistState(uint id, DbObject obj);
        bool SetInLot(uint id, uint? lot_id);

        int ChangeInventoryOwners(IEnumerable<uint> object_ids, uint oldOwner, uint newOwner);
        
        List<DbObjectAttribute> GetObjectAttributes(List<uint> objects);
        int GetSpecificObjectAttribute(uint objectID, int index);
        void SetObjectAttributes(List<DbObjectAttribute> attrs);
        int TotalObjectAttributes(uint guid, int index);
    }
}
