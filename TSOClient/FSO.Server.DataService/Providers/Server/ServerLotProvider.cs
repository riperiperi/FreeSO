using FSO.Common.DataService.Framework;
using FSO.Common.DataService.Model;
using FSO.Common.Domain.Realestate;
using FSO.Common.Domain.RealestateDomain;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.Lots;
using FSO.Server.Database.DA.Shards;
using FSO.Server.Protocol.CitySelector;
using Ninject;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FSO.Common.Utils;
using FSO.Common.Security;
using System.Security;

namespace FSO.Common.DataService.Providers.Server
{
    public class ServerLotProvider : EagerDataServiceProvider<uint, Lot>
    {
        private IShardRealestateDomain Realestate;
        private int ShardId;
        private IDAFactory DAFactory;
        
        public ServerLotProvider([Named("ShardId")] int shardId, IRealestateDomain realestate, IDAFactory daFactory){
            OnMissingLazyLoad = true;
            OnLazyLoadCacheValue = false;

            ShardId = shardId;
            Realestate = realestate.GetByShard(shardId);
            DAFactory = daFactory;
        }

        protected override void PreLoad(Callback<uint, Lot> appender)
        {
            using (var db = DAFactory.Get())
            {
                var all = db.Lots.All(ShardId);
                foreach(var item in all){
                    var converted = HydrateOne(item);
                    var intId = MapCoordinates.Pack(converted.Lot_Location.Location_X, converted.Lot_Location.Location_Y);
                    appender(intId, converted);
                }
            }
        }

        protected Lot HydrateOne(DbLot lot)
        {
            var location = MapCoordinates.Unpack(lot.location);

            var result = new Lot
            {
                Lot_Name = lot.name,
                Lot_IsOnline = false,
                Lot_Location = new Location { Location_X = location.X, Location_Y = location.Y },
                Lot_Price = (uint)Realestate.GetPurchasePrice(location.X, location.Y),
                Lot_LeaderID = lot.owner_id,
                Lot_OwnerVec = new List<uint>() { lot.owner_id },
                Lot_RoommateVec = new List<uint>() { },
                Lot_NumOccupants = 0,
                Lot_LastCatChange = lot.category_change_date,
                Lot_Description = lot.description
            };

            return result;
        }

        //Should only get here for non-occupied lots that just need a price, we can avoid caching these
        protected override Lot LazyLoad(uint key)
        {
            var location = MapCoordinates.Unpack(key);

            //Empty lot
            return new Lot
            {
                Lot_IsOnline = false,
                Lot_Location = new Location { Location_X = location.X, Location_Y = location.Y },
                //Lot_Price = 0,
                Lot_Price = (uint)Realestate.GetPurchasePrice(location.X, location.Y),
                Lot_OwnerVec = new List<uint>() { },
                Lot_RoommateVec = new List<uint>() { }
            };
        }

        public override void PersistMutation(object entity, MutationType type, string path, object value)
        {
            var lot = entity as Lot;

            switch (path){
                case "Lot_Category":
                    break;
            }
        }

        public override void DemandMutation(object entity, MutationType type, string path, object value, ISecurityContext context)
        {
            var lot = entity as Lot;

            switch (path)
            {
                //Owner only
                case "Lot_Description":
                case "Lot_Category":
                    context.DemandAvatar(lot.Lot_LeaderID, AvatarPermissions.WRITE);
                    break;

                default:
                    throw new SecurityException("Field: " + path + " may not be mutated by users");
            }
        }
    }
}
