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
using System.IO;
using FSO.Common.Serialization.Primitives;
using FSO.Server.Database.DA.Roommates;

namespace FSO.Common.DataService.Providers.Server
{
    public class ServerLotProvider : EagerDataServiceProvider<uint, Lot>
    {
        private Dictionary<string, Lot> LotsByName = new Dictionary<string, Lot>();
        public City CityRepresentation;

        private IRealestateDomain GlobalRealestate;
        private IShardRealestateDomain Realestate;
        private int ShardId;
        private IDAFactory DAFactory;
        private IServerNFSProvider NFS;
        
        public ServerLotProvider([Named("ShardId")] int shardId, IRealestateDomain realestate, IDAFactory daFactory, IServerNFSProvider nfs)
        {
            OnMissingLazyLoad = true;
            OnLazyLoadCacheValue = false;

            ShardId = shardId;
            GlobalRealestate = realestate;
            Realestate = realestate.GetByShard(shardId);
            DAFactory = daFactory;
            NFS = nfs;
            CityRepresentation = new City()
            {
                City_NeighborhoodsVec = new List<uint>(),
                City_OnlineLotVector = new List<bool>(),
                City_ReservedLotVector = new List<bool>(),
                City_ReservedLotInfo = new Dictionary<uint, bool>(),
                City_SpotlightsVector = new List<uint>(),
                City_Top100ListIDs = new List<uint>(),
                City_TopTenNeighborhoodsVector = new List<uint>()
            };
        }

        protected override void PreLoad(Callback<uint, Lot> appender)
        {
            using (var db = DAFactory.Get())
            {
                var all = db.Lots.All(ShardId);
                foreach(var item in all){
                    var roommates = db.Roommates.GetLotRoommates(item.lot_id);
                    var converted = HydrateOne(item, roommates);
                    var intId = MapCoordinates.Pack(converted.Lot_Location.Location_X, converted.Lot_Location.Location_Y);
                    appender(intId, converted);
                }
            }
        }

        protected override Lot LoadOne(uint key)
        {
            using (var db = DAFactory.Get())
            {
                var lot = db.Lots.GetByLocation(ShardId, key);
                if (lot == null) return null;
                else
                {
                    var roommates = db.Roommates.GetLotRoommates(lot.lot_id);
                    return HydrateOne(lot, roommates);
                }
            }
        }

        protected override void Insert(uint key, Lot value)
        {
            base.Insert(key, value);
            LotsByName[value.Lot_Name] = value;
            CityRepresentation.City_ReservedLotInfo[value.Lot_Location_Packed] = value.Lot_IsOnline; //TODO: thread-safe all maps
        }

        protected Lot HydrateOne(DbLot lot, List<DbRoommate> roommates)
        {
            var location = MapCoordinates.Unpack(lot.location);

            //attempt to load the lot's thumbnail.
            var path = Path.Combine(NFS.GetBaseDirectory(), "Lots/" + lot.lot_id.ToString("x8") + "/thumb.png");
            cTSOGenericData thumb = null;
            try
            {
                using (FileStream fs = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    int numBytesToRead = Convert.ToInt32(fs.Length);
                    var file = new byte[(numBytesToRead)];
                    fs.Read(file, 0, numBytesToRead);
                    thumb = new cTSOGenericData(file);
                }
            }
            catch (Exception) {
                thumb = new cTSOGenericData(new byte[0]);
            }

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
                Lot_RoommateVec = new List<uint>(),
                Lot_NumOccupants = 0,
                Lot_LastCatChange = lot.category_change_date,
                Lot_Description = lot.description,
                Lot_Thumbnail = thumb
            };

            foreach (var roomie in roommates)
            {
                if (roomie.is_pending == 0) result.Lot_RoommateVec.Add(roomie.avatar_id);
            }

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
                Lot_RoommateVec = new List<uint>() { },

                Lot_Thumbnail = new Serialization.Primitives.cTSOGenericData(new byte[0]),
                Lot_ThumbnailCheckSum = key
            };
        }

        public override void PersistMutation(object entity, MutationType type, string path, object value)
        {
            var lot = entity as Lot;

            switch (path){
                case "Lot_Description":
                    using (var db = DAFactory.Get())
                    {
                        db.Lots.UpdateDescription(lot.DbId, lot.Lot_Description);
                    }
                    break;
                case "Lot_Thumbnail":
                    var imgpath = Path.Combine(NFS.GetBaseDirectory(), "Lots/" + lot.DbId.ToString("x8") + "/thumb.png");
                    var data = (cTSOGenericData)value;

                    using (FileStream fs = File.Open(imgpath, FileMode.Create, FileAccess.Write, FileShare.None))
                    {
                        fs.Write(data.Data, 0, data.Data.Length);
                    }
                    break;
                case "Lot_Category":
                    break;
                case "Lot_IsOnline":
                    CityRepresentation.City_ReservedLotInfo[lot.Lot_Location_Packed] = lot.Lot_IsOnline;
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
                    var desc = value as string;
                    if (desc != null && desc.Length > 500)
                        throw new Exception("Description too long!");
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

                //roommate only
                case "Lot_Thumbnail":
                    var roomies = lot.Lot_RoommateVec;
                    context.DemandAvatars(roomies, AvatarPermissions.WRITE);
                    //TODO: needs to be generic data, png, size 288x288, less than 1MB
                    break;
                case "Lot_IsOnline":
                case "Lot_NumOccupants":
                case "Lot_RoommateVec":
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
