using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Lots
{
    public interface ILots
    {
        DbLot GetByOwner(uint owner_id);
        DbLot Get(uint id);
        uint Create(DbLot lot);
    }
}
