using FSO.Server.Database.DA.Hosts;
using FSO.Server.Framework.Gluon;
using FSO.Server.Protocol.Gluon.Packets;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FSO.Server.Domain
{
    public interface IGluonHostPool
    {
        string PoolHash { get; }

        IGluonHost Get(string callSign);
        IGluonHost GetByShardId(int shard_id);
        IEnumerable<IGluonHost> GetByRole(DbHostRole role);
        IEnumerable<IGluonHost> GetAll();

        void Start();
        void Stop();
    }

    public interface IGluonHost : IGluonSession
    {
        DbHostRole Role { get; }
        bool Connected { get; }
        DateTime BootTime { get; }
        Task<IGluonCall> Call<IN>(IN input) where IN : IGluonCall;
    }
}
