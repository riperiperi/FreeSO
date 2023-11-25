using System.Collections.Generic;

namespace FSO.Server.Database.DA.Hosts
{
    public interface IHosts
    {
        IEnumerable<DbHost> All();
        DbHost Get(string call_sign);
        void CreateHost(DbHost host);
        void SetStatus(string call_sign, DbHostStatus status);
    }
}
