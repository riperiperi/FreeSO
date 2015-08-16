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
    }
}
