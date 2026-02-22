using FSO.Common.Enum;

namespace FSO.Server.Database.DA.Lots
{
    [Flags]
    public enum LotMoveFlags
    {
        /// <summary>
        /// This lot has moved.
        /// Flatten the buildable area, regenerate the terrain.
        /// </summary>
        Moved = 1,

        /// <summary>
        /// This lot is new, or being reset as new.
        /// The terrain will be fully reset, and unowned objects will be placed on it.
        /// </summary>
        New = 1 << 1,

        /// <summary>
        /// This lot is being deleted when the lot container shuts down.
        /// Typically when a lot has this flag, it was opened to migrate all roomie objects into their inventories.
        /// </summary>
        PermanentDelete = 1 << 2,

        /// <summary>
        /// Similar to Moved, but doesn't flatten the buildable area.
        /// Triggers when the city terrain is changed around this lot.
        /// </summary>
        TerrainRegen = 1 << 3,

        ShouldClearObjects = New | PermanentDelete
    }

    [Flags]
    public enum LotArchiveFlags
    {
        /// <summary>
        /// Archive a property from an old save.
        /// Objects unowned by roommates should be transformed into ownerless objects.
        /// The terrain should be recalculated without damaging the buildable area.
        /// After loading, the archive flags change to 2.
        /// </summary>
        ArchiveFromOldSave = 1,

        /// <summary>
        /// Some special rules for archive lots.
        /// The object limit disable isn't active, similar to community lots.
        /// </summary>
        ArchiveRules = 1 << 1,
    }

    public class DbLot
    {
        public int lot_id { get; set; }
        public int shard_id { get; set; }
        public uint? owner_id { get; set; }

        public string name { get; set; }
        public string description { get; set; }
        public uint location { get; set; }
        public uint neighborhood_id { get; set; }
        public uint created_date { get; set; }
        public uint category_change_date { get; set; }
        public LotCategory category { get; set; }
        public byte skill_mode { get; set; }
        public uint buildable_area { get; set; }
        public sbyte ring_backup_num { get; set; }
        public byte admit_mode { get; set; }
        public byte move_flags { get; set; }

        public byte thumb3d_dirty { get; set; }
        public uint thumb3d_time { get; set; }

        // Added for archive
        public byte archive_flags { get; set; }

        public LotMoveFlags MoveFlags
        {
            get
            {
                return (LotMoveFlags)move_flags;
            }
            set
            {
                move_flags = (byte)value;
            }
        }

        public LotArchiveFlags ArchiveFlags
        {
            get
            {
                return (LotArchiveFlags)archive_flags;
            }
            set
            {
                archive_flags = (byte)value;
            }
        }
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
