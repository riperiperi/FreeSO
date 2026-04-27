using FSO.Content.Model;
using FSO.Server.Protocol.CitySelector;

namespace FSO.Common.Domain.Shards
{
    public class ClientShards : IShardsDomain
    {
        private Dictionary<int, CityMap> CustomMapsByShard = [];
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

        public void SetShardMapBase(int id, CityMapMarshal marshal)
        {
            CustomMapsByShard[id] = new CityMap(marshal);
        }

        public CityMap GetMapForId(int id)
        {
            var shard = GetById(id);

            if (shard.Map.StartsWith("dynamic"))
            {
                return CustomMapsByShard[id];
            }

            return FSO.Content.Content.Get().CityMaps.Get(shard.Map);
        }
    }
}
