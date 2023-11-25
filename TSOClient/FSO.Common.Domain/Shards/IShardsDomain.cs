using FSO.Server.Protocol.CitySelector;
using System.Collections.Generic;

namespace FSO.Common.Domain.Shards
{
    public interface IShardsDomain
    {
        List<ShardStatusItem> All { get; }
        ShardStatusItem GetById(int id);
        ShardStatusItem GetByName(string name);
        int? CurrentShard { get; }
    }
}
