using System.Collections.Generic;

namespace FSO.Server.Database.DA.Outfits
{
    public interface IOutfits
    {
        uint Create(DbOutfit outfit);
        List<DbOutfit> GetByObjectId(uint object_id);
        List<DbOutfit> GetByAvatarId(uint avatar_id);

        bool UpdatePrice(uint outfit_id, uint object_id, int new_price);
        bool ChangeOwner(uint outfit_id, uint object_owner, uint new_avatar_owner);
        bool DeleteFromObject(uint outfit_id, uint object_id);
        bool DeleteFromAvatar(uint outfit_id, uint avatar_id);
    }
}
