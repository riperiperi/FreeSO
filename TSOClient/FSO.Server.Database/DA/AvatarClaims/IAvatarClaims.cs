using System.Collections.Generic;

namespace FSO.Server.Database.DA.AvatarClaims
{
    public interface IAvatarClaims
    {
        DbAvatarClaim Get(int id);
        IEnumerable<DbAvatarClaim> GetAll();
        IEnumerable<DbAvatarActive> GetAllActiveAvatars();
        int? GetAllActiveAvatarsCount();
        DbAvatarClaim GetByAvatarID(uint id);
        IEnumerable<DbAvatarClaim> GetAllByOwner(string owner);

        int? TryCreate(DbAvatarClaim claim);
        bool Claim(int id, string previousOwner, string newOwner, uint location);
        void RemoveRemaining(string previousOwner, uint location);

        void Delete(int id, string owner);
        void DeleteAll(string owner);
    }
}
