using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Lots
{
    public class DbLot
    {
        public int lot_id { get; set; }
        public int shard_id { get; set; }
        public uint owner_id { get; set; }

        public string name { get; set; }
        public string description { get; set; }
        public uint location { get; set; }
        public uint neighborhood_id { get; set; }
        public uint created_date { get; set; }
        public uint category_change_date { get; set; }
        public DbLotCategory category { get; set; }
        public uint buildable_area { get; set; }
        public sbyte ring_backup_num { get; set; }
        public byte admit_mode { get; set; }
    }

    public enum DbLotCategory
    {
        none = 0,
        welcome = 1,
        money = 2,
        skills = 3,
        services = 4,
        entertainment = 5,
        romance = 6,
        shopping = 7,
        games = 8,
        offbeat = 9,
        residence = 10
    }

    /**Lot
	Lot_BuildableArea : Uint32 (0)
	Lot_NumOccupants : Uint8 (0)
	Lot_SpotLightText : string (0)
	Lot_Location : Location (0)
	Lot_NeighborhoodCentered : Uint32 (0)
	Lot_Thumbnail : iunknown (0)
	Lot_NeighborhoodName : string (0)
	Lot_NeighborhoodID : Uint32 (0)
	Lot_OwnerVec : Uint32 (2)
	Lot_IsOnline : bool (0)
	Lot_TerrainType : Uint32 (0)
	Lot_LeaderID : Uint32 (0)
	Lot_Name : string (0)
	Lot_DBID : Uint32 (0)
	Lot_PossibleNeighborhoodsVector : Uint32 (2)
	Lot_RoommateVec : Uint32 (2)
	Lot_LotAdmitInfo : LotAdmitInfo (0)
	Lot_Description : string (0)
	Lot_Price : Uint32 (0)
	Lot_HoursSinceLastLotCatChange : Uint32 (0)
	Lot_ThumbnailCheckSum : Uint32 (0)
	Lot_Category : Uint8 (0)**/
}
