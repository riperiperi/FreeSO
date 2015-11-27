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
        private Dictionary<string, Lot> LotsByName = new Dictionary<string, Lot>();

        private IRealestateDomain GlobalRealestate;
        private IShardRealestateDomain Realestate;
        private int ShardId;
        private IDAFactory DAFactory;
        
        public ServerLotProvider([Named("ShardId")] int shardId, IRealestateDomain realestate, IDAFactory daFactory){
            OnMissingLazyLoad = true;
            OnLazyLoadCacheValue = false;

            ShardId = shardId;
            GlobalRealestate = realestate;
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

        protected override void Insert(uint key, Lot value)
        {
            base.Insert(key, value);
            LotsByName[value.Lot_Name] = value;
        }

        protected Lot HydrateOne(DbLot lot)
        {
            var location = MapCoordinates.Unpack(lot.location);

            var result = new Lot
            {
                DbId = lot.lot_id,
                Id = lot.location,

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
                Id = key,

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
            if (lot.DbId == 0) { throw new SecurityException("Unclaimed lots cannot be mutated"); }

            switch (path)
            {
                //Owner only
                case "Lot_Description":
                    context.DemandAvatar(lot.Lot_LeaderID, AvatarPermissions.WRITE);
                    break;

                case "Lot_Name":
                    context.DemandAvatar(lot.Lot_LeaderID, AvatarPermissions.WRITE);
                    if (!GlobalRealestate.ValidateLotName((string)value)){
                        throw new Exception("Invalid lot name");
                    }
                    //Lot_Name is a special case, it has to be unique so we have to hit the db in the security check
                    //for this mutation.
                    TryChangeLotName(lot, (string)value);
                    break;

                case "Lot_Category":
                    context.DemandAvatar(lot.Lot_LeaderID, AvatarPermissions.WRITE);
                    //7 days
                    if (lot.Lot_HoursSinceLastLotCatChange < 168){
                        throw new SecurityException("You must wait 7 days to change your lot category again");
                    }
                    break;

                case "Lot_IsOnline":
                case "Lot_NumOccupants":
                    context.DemandInternalSystem();
                    break;

                default:
                    throw new SecurityException("Field: " + path + " may not be mutated by users");
            }
        }

        private void TryChangeLotName(Lot lot, string name)
        {
            using (var db = DAFactory.Get())
            {
                //The DB will enforce uniqueness per shard
                db.Lots.RenameLot(lot.DbId, name);
            }
        }

        public Lot GetByName(string name)
        {
            if (LotsByName.ContainsKey(name))
            {
                return LotsByName[name];
            }
            return null;
        }
    }
}
