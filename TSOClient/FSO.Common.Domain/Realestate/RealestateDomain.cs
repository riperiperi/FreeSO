using FSO.Common.Domain.RealestateDomain;
using FSO.Common.Domain.Shards;
using FSO.Content.Model;
using FSO.Server.Protocol.CitySelector;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace FSO.Common.Domain.Realestate
{
    public class RealestateDomain : IRealestateDomain
    {
        // No need to check redundant regex conditions until you have to throw various errors
        // (vide tso.client/UI/Panels/UILotPurchaseDialog.cs)
        // I tried to combine conditions to reduce redundancy
        private Regex VALIDATE_SPECIAL_CHARS = new Regex(@"[^\p{L} '-]"); // Numbers are special chars in this case
        private Regex VALIDATE_APOSTROPHES = new Regex("^[^']*'?[^']*$");
        private Regex VALIDATE_DASHES = new Regex("^[^-]*-?[^-]*$");
        private Regex VALIDATE_SPACES = new Regex("^[^ ]+(?: [^ ]+)*$");

        private Dictionary<int, ShardRealestateDomain> _ByShard;
        private IShardsDomain _Shards;
        private FSO.Content.Content _Content;

        public RealestateDomain(IShardsDomain shards, FSO.Content.Content content)
        {
            _Shards = shards;
            _Content = content;
            _ByShard = new Dictionary<int, ShardRealestateDomain>();
            
            foreach(var item in shards.All){
                GetByShard(item.Id);
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
                var item = new ShardRealestateDomain(shard, this._Content.CityMaps.Get(shard.Map));
                _ByShard.Add(shardId, item);
                return item;
            }
        }

        public bool ValidateLotName(string name)
        {
            if (string.IsNullOrEmpty(name) ||
                name.Length < 3 ||
                name.Length > 24 ||
                VALIDATE_SPECIAL_CHARS.IsMatch(name) ||
                !VALIDATE_APOSTROPHES.IsMatch(name) ||
                !VALIDATE_DASHES.IsMatch(name) ||
                !VALIDATE_SPACES.IsMatch(name))
            {
                return false;
            }
            return true;
        }
    }

    public class ShardRealestateDomain : IShardRealestateDomain
    {
        private LotPricingStrategy _Pricing;
        private CityMap _Map;

        public ShardRealestateDomain(ShardStatusItem shard, CityMap map)
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
            if(!MapCoordinates.InBounds(x, y, 1)){
                //Out of bounds!
                return false;
            }

            //Cant build on water
            var terrain = _Map.GetTerrain(x, y);
            if (terrain == TerrainType.WATER) { return false; }

            var slope = GetSlope(x, y);

            //10 is threshold for now
            return (slope < 10);
        }

        public int GetSlope(ushort x, ushort y)
        {
            x += 1;
            //Check elevation is ok, get all 4 corners and then decide
            var tl = _Map.GetElevation(x, y);
            var trPoint = MapCoordinates.Offset(x, y, 1, 0);
            var tr = _Map.GetElevation(trPoint.X, trPoint.Y);
            var blPoint = MapCoordinates.Offset(x, y, 0, 1);
            var bl = _Map.GetElevation(blPoint.X, blPoint.Y);
            var brPoint = MapCoordinates.Offset(x, y, 1, 1);
            var br = _Map.GetElevation(brPoint.X, brPoint.Y);

            int max = Math.Max(tl, Math.Max(tr, Math.Max(bl, br)));
            int min = Math.Min(tl, Math.Min(tr, Math.Min(bl, br)));

            return (max - min);
        }

        public CityMap GetMap()
        {
            return _Map;
        }
    }
}
