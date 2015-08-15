using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA
{
    public interface ISqlContext : IDisposable
    {
        DbConnection Connection { get; }
    }
}
