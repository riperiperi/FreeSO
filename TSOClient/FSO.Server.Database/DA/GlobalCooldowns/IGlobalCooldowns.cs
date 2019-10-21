using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.GlobalCooldowns
{
    public interface IGlobalCooldowns
    {
        DbGlobalCooldowns Get(uint objguid, uint avatarid, bool useAccount, uint category);
        List<DbGlobalCooldowns> GetAllByObj(uint objguid);
        List<DbGlobalCooldowns> GetAllByAvatar(uint avatarid);
        List<DbGlobalCooldowns> GetAllByObjectAndAvatar(uint objguid, uint avatarid);
        bool Create(DbGlobalCooldowns cooldown);
        bool Update(DbGlobalCooldowns cooldown);
    }
}
