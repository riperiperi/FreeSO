using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.Shards;
using FSO.Common.Security;
using FSO.Common.Serialization.Primitives;
using FSO.Common.Utils.Cache;

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

        /// <summary>
        /// This is called before values are updated. We can use this to compare the thumbnail and write it to the cache if its changed
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="type"></param>
        /// <param name="path"></param>
        /// <param name="value"></param>
        /// <param name="context"></param>
        public override void DemandMutation(object entity, MutationType type, string path, object value, ISecurityContext context)
        {
            if(path == "Lot_Thumbnail")
            {
                var lot = (Lot)entity;
                var newValue = (cTSOGenericData)value;
                var oldValue = lot.Lot_Thumbnail;

                var persist = false;

                if (newValue != null)
                {
                    if(oldValue == null){
                        persist = true;
                    }else{
                        persist = !FastBytesCompare(oldValue.Data, newValue.Data);
                    }
                }


                if (persist && Shards.CurrentShard.HasValue){
                    var key = CacheKey.For("shards", Shards.CurrentShard.Value, "lot_thumbs", lot.Id);
                    Cache.Add(key, ((cTSOGenericData)value).Data);
                }
            }
        }


        private bool FastBytesCompare(byte[] a, byte[] b)
        {
            if (a == null && b != null) { return false; }
            if (a != null && b == null) { return false; }
            if (a.Length != b.Length) { return false; }

            for(var i=0; i < a.Length; i++){
                if(a[i] != b[i]){
                    return false;
                }
            }

            return true;
        }

    }
}
