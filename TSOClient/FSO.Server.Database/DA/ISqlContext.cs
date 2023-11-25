using System;
using System.Data.Common;

namespace FSO.Server.Database.DA
{
    public interface ISqlContext : IDisposable
    {
        DbConnection Connection { get; }
        void Flush();
    }
}
