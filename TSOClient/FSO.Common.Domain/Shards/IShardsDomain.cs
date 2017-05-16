using FSO.Server.Protocol.CitySelector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
