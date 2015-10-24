using FSO.Server.Database.DA.Shards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Domain.Shards
{
    public interface IShards
    {
        List<Shard> All { get; }
        Shard GetById(int id);
        Shard GetByName(string name);
    }
}
