using FSO.Common.Domain.RealestateDomain;
using FSO.Common.Domain.Shards;
using FSO.Content.Model;
using FSO.Server.Database.DA.Shards;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Domain.Realestate
{
    public class RealestateDomain : IRealestateDomain
    {
        private Dictionary<int, ShardRealestateDomain> _ByShard;
        private IShardsDomain _Shards;
        private FSO.Content.Content _Content;

        public RealestateDomain(IShardsDomain shards, FSO.Content.Content content)
        {
            _Shards = shards;
            _Content = content;
            _ByShard = new Dictionary<int, ShardRealestateDomain>();
            
            foreach(var item in shards.All){
                GetByShard(item.shard_id);
            }
        }

        public IShardRealestateDomain GetByShard(int shardId)
        {
            lock (_ByShard)
            {
                if (_ByShard.ContainsKey(shardId))
                {
                    return _ByShard[shardId];
                }

                var shard = _Shards.GetById(shardId);
                var item = new ShardRealestateDomain(shard, this._Content.CityMaps.Get(shard.map));
                _ByShard.Add(shardId, item);
                return item;
            }
        }
    }

    public class ShardRealestateDomain : IShardRealestateDomain
    {
        private LotPricingStrategy _Pricing;
        private CityMap _Map;

        public ShardRealestateDomain(Shard shard, CityMap map)
        {
            _Map = map;
            //TODO: Hardcore
            _Pricing = new BasicLotPricingStrategy();
        }

        public int GetPurchasePrice(ushort x, ushort y)
        {
            return _Pricing.GetPrice(_Map, x, y);
        }

        public bool IsPurchasable(ushort x, ushort y)
        {
            //Cant buy lots on the very edge
            if (x < 1 || y < 1) { return false; }
            if (x > 304 || y > 203) { return false; }
            
            //Cant buy water lots
            var terrain = _Map.GetTerrain(x, y);
            return terrain != TerrainType.WATER;
        }
    }
}
