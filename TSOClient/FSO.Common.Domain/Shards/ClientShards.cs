using System.Collections.Generic;
using System.Linq;
using FSO.Content.Model;
using FSO.Server.Protocol.CitySelector;

namespace FSO.Common.Domain.Shards
{
    public class ClientShards : IShardsDomain
    {
        public int? CurrentShard { get; set; }

        public List<ShardStatusItem> All
        {
            get; set;
        } = new List<ShardStatusItem>();

        public ShardStatusItem GetById(int id)
        {
            return All.FirstOrDefault(x => x.Id  == id);
        }

        public ShardStatusItem GetByName(string name)
        {
            return All.FirstOrDefault(x => x.Name == name);
        }

        public CityMap GetMapForId(int id)
        {
            var shard = GetById(id);

            if (shard.Map.StartsWith("dynamic"))
            {
                // TODO: Load map from server (the client _must_ fetch the shard's map first)
            }

            return FSO.Content.Content.Get().CityMaps.Get(shard.Map);
        }
    }
}
