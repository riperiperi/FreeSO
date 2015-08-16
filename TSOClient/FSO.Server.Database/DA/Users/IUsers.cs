using FSO.Server.Database.DA.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Users
{
    public interface IUsers
    {
        User GetById(uint id);
        User GetByUsername(string username);
        UserAuthenticate GetAuthenticationSettings(uint userId);
        PagedList<User> All(int offset = 0, int limit = 20, string orderBy = "register_date");
        uint Create(User user);
    }
}
