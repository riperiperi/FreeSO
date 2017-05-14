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
using FSO.Server.Database.DA.LotAdmit;
using System.Collections.Immutable;
using FSO.Common.Enum;
using FSO.Server.Common;

namespace FSO.Server.DataService.Providers
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
                City_NeighborhoodsVec = ImmutableList.Create<uint>(),
                City_OnlineLotVector = ImmutableList.Create<bool>(),
                City_ReservedLotVector = ImmutableList.Create<bool>(),
                City_ReservedLotInfo = new Dictionary<uint, bool>(),
                City_SpotlightsVector = ImmutableList.Create<uint>(),
                City_Top100ListIDs = ImmutableList.Create<uint>(),
                City_TopTenNeighborhoodsVector = ImmutableList.Create<uint>()
            };
        }

        protected override void PreLoad(Callback<uint, Lot> appender)
        {
            using (var db = DAFactory.Get())
            {
                var all = db.Lots.All(ShardId);
                foreach(var item in all){
                    var roommates = db.Roommates.GetLotRoommates(item.lot_id);
                    var admit = db.LotAdmit.GetLotInfo(item.lot_id);
                    var converted = HydrateOne(item, roommates, admit);
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
                    var admit = db.LotAdmit.GetLotInfo(lot.lot_id);
                    return HydrateOne(lot, roommates, admit);
                }
            }
        }

        protected override void Insert(uint key, Lot value)
        {
            base.Insert(key, value);
            lock (LotsByName) LotsByName[value.Lot_Name] = value;
            lock (CityRepresentation.City_ReservedLotInfo) CityRepresentation.City_ReservedLotInfo[value.Lot_Location_Packed] = value.Lot_IsOnline;
        }

        protected override Lot Remove(uint key)
        {
            var value = base.Remove(key);
            if (value != null)
            {
                lock (LotsByName) LotsByName.Remove(value.Lot_Name);
                lock (CityRepresentation.City_ReservedLotInfo) CityRepresentation.City_ReservedLotInfo.Remove(value.Lot_Location_Packed);
                
                CityRepresentation.City_SpotlightsVector = CityRepresentation.City_SpotlightsVector.Remove(value.Lot_Location_Packed);
            }
            return value;
        }

        protected Lot HydrateOne(DbLot lot, List<DbRoommate> roommates, List<DbLotAdmit> admit)
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
                Lot_OwnerVec = ImmutableList.Create(lot.owner_id),
                Lot_RoommateVec = ImmutableList.Create<uint>(),
                Lot_LotAdmitInfo = new LotAdmitInfo() { LotAdmitInfo_AdmitMode = lot.admit_mode },
                Lot_NumOccupants = 0,
                Lot_Category = (byte)lot.category,
                Lot_LastCatChange = lot.category_change_date,
                Lot_Description = lot.description,
                Lot_Thumbnail = thumb
            };

            foreach (var roomie in roommates)
            {
                if (roomie.is_pending == 0) result.Lot_RoommateVec = result.Lot_RoommateVec.Add(roomie.avatar_id);
            }

            var admitL = new List<uint>();
            var banL = new List<uint>();
            foreach (var item in admit)
            {
                if (item.admit_type == 0) admitL.Add(item.avatar_id);
                else banL.Add(item.avatar_id);
            }
            result.Lot_LotAdmitInfo.LotAdmitInfo_AdmitList = ImmutableList.ToImmutableList(admitL);
            result.Lot_LotAdmitInfo.LotAdmitInfo_BanList = ImmutableList.ToImmutableList(banL);

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
                Lot_OwnerVec = ImmutableList.Create<uint>(),
                Lot_RoommateVec = ImmutableList.Create<uint>(),

                Lot_Thumbnail = new cTSOGenericData(new byte[0]),
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
                    using (var db = DAFactory.Get())
                    {
                        db.Lots.UpdateLotCategory(lot.DbId, (LotCategory)(lot.Lot_Category));
                    }
                    break;
                case "Lot_IsOnline":
                    lock (CityRepresentation.City_ReservedLotInfo) CityRepresentation.City_ReservedLotInfo[lot.Lot_Location_Packed] = lot.Lot_IsOnline;
                    break;
                case "Lot_SpotLightText":
                    lock (CityRepresentation)
                    {
                        var clone = new HashSet<uint>(CityRepresentation.City_SpotlightsVector); //need to convert this to a hashset to add to it properly
                        if (lot.Lot_SpotLightText != "") clone.Add(lot.Lot_Location_Packed);
                        else clone.Remove(lot.Lot_Location_Packed);
                        CityRepresentation.City_SpotlightsVector = ImmutableList.ToImmutableList(clone);
                    }
                    break;
                case "Lot_LotAdmitInfo.LotAdmitInfo_AdmitMode":
                    using (var db = DAFactory.Get())
                    {
                        db.Lots.UpdateLotAdmitMode(lot.DbId, (byte)value);
                    }
                    break;
            }
        }

        public override void DemandMutation(object entity, MutationType type, string path, object value, ISecurityContext context)
        {
            var lot = entity as Lot;
            if (lot.DbId == 0) { throw new SecurityException("Unclaimed lots cannot be mutated"); }

            var roomies = lot.Lot_RoommateVec;
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

                    if (lot.Lot_IsOnline) throw new SecurityException("Lot must be offline to change category!");

                    //7 days
                    if (((Epoch.Now - lot.Lot_LastCatChange) / (60 * 60)) < 168){
                        throw new SecurityException("You must wait 7 days to change your lot category again");
                    }
                    break;

                //roommate only
                case "Lot_Thumbnail":
                    context.DemandAvatars(roomies, AvatarPermissions.WRITE);
                    //TODO: needs to be generic data, png, size 288x288, less than 1MB
                    break;
                case "Lot_IsOnline":
                case "Lot_NumOccupants":
                case "Lot_RoommateVec":
                case "Lot_SpotLightText":
                    context.DemandInternalSystem();
                    break;
                case "Lot_LotAdmitInfo.LotAdmitInfo_AdmitList":
                case "Lot_LotAdmitInfo.LotAdmitInfo_BanList":
                    context.DemandAvatars(roomies, AvatarPermissions.WRITE);
                    int atype = (path == "Lot_LotAdmitInfo.LotAdmitInfo_AdmitList") ? 0 : 1;
                    using (var db = DAFactory.Get())
                    { //need to check db constraints
                        switch (type)
                        {
                            case MutationType.ARRAY_REMOVE_ITEM:
                                //Remove bookmark at index value
                                var removedAva = (uint)value;
                                db.LotAdmit.Delete(new DbLotAdmit
                                {
                                    lot_id = (int)lot.DbId,
                                    avatar_id = removedAva,
                                    admit_type = (byte)atype
                                });
                                break;
                            case MutationType.ARRAY_SET_ITEM:
                                //Add a new bookmark
                                var newAva = (uint)value;
                                db.LotAdmit.Create(new DbLotAdmit
                                {
                                    lot_id = (int)lot.DbId,
                                    avatar_id = newAva,
                                    admit_type = (byte)atype
                                });
                                break;
                        }
                    }
                    break;
                case "Lot_LotAdmitInfo.LotAdmitInfo_AdmitMode":
                    context.DemandAvatars(roomies, AvatarPermissions.WRITE);
                    //can only set valid values
                    var mode = (byte)value;
                    if (mode < 0 || mode > 3) 
                        throw new Exception("Invalid admit mode!");
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
            lock (LotsByName)
            {
                if (LotsByName.ContainsKey(name))
                {
                    return LotsByName[name];
                }
            }
            return null;
        }
    }
}
