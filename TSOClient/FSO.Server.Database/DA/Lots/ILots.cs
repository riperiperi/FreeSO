using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Lots
{
    public interface ILots
    {
        IEnumerable<DbLot> All(int shard_id);
        DbLot GetByLocation(int shard_id, uint location);
        DbLot GetByOwner(uint owner_id);
        DbLot Get(uint id);
        uint Create(DbLot lot);
    }
}
