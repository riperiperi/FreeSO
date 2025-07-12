using System;
using System.Data.Common;

namespace FSO.Server.Database.DA
{
    public interface ISqlContext : IDisposable
    {
        bool SupportsFunctions { get; }
        bool UseBlobInventory { get; }
        DbConnection Connection { get; }
        void Flush();
        string CompatLayer(string sql, string updateKey = null);
    }
}
