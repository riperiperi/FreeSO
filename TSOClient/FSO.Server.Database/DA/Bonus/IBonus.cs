using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Bonus
{
    public interface IBonus
    {
        IEnumerable<DbBonus> GetByAvatarId(uint avatar_id);
    }
}
