using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.LotClaims
{
    public interface ILotClaims
    {
        uint? TryCreate(DbLotClaim claim);
        IEnumerable<DbLotClaim> GetAllByOwner(string owner);

        bool Claim(uint id, string previousOwner, string newOwner);
        DbLotClaim Get(uint id);
        DbLotClaim GetByLotID(int id);

        void RemoveAllByOwner(string owner);
        void Delete(uint id, string owner);
    }
}
