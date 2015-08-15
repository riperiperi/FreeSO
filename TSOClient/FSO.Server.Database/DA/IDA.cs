using FSO.Server.Database.DA.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA
{
    public interface IDA : IDisposable
    {
        IUsers Users { get; }
    }
}
