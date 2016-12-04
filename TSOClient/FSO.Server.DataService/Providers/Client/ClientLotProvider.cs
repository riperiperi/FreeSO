using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.Shards;
using FSO.Common.Serialization.Primitives;
using FSO.Common.Utils.Cache;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.DataService.Providers.Client
{
    public class ClientLotProvider : ReceiveOnlyServiceProvider<uint, Lot>
    {
        private ICache Cache;
        private IShardsDomain Shards;

        public ClientLotProvider(ICache cache, IShardsDomain shards)
        {
            this.Shards = shards;
            this.Cache = cache;
        }

        protected override Lot CreateInstance(uint key)
        {
            var coords = MapCoordinates.Unpack(key);

            var lot = base.CreateInstance(key);
            lot.Id = key;
            lot.Lot_Location = new Location()
            {
                Location_X = coords.X,
                Location_Y = coords.Y
            };
            //TODO: Use the string tables
            lot.Lot_Name = "Retrieving...";
            return lot;
        }

        public override void PersistMutation(object entity, MutationType type, string path, object value)
        {
            if(path == "Lot_Thumbnail")
            {
                var lot = (Lot)entity;
                if (Shards.CurrentShard.HasValue){
                    var key = CacheKey.For("shards", Shards.CurrentShard.Value, "lot_thumbs", lot.Id);
                    Cache.Add(key, ((cTSOGenericData)value).Data);
                }
            }
        }
    }
}
