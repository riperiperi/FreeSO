using System;

namespace FSO.Server.Database.DA.Outfits
{
    public class DbOutfit
    {
        public uint outfit_id { get; set; }
        public Nullable<uint> avatar_owner { get; set; }
        public Nullable<uint> object_owner { get; set; }
        public ulong asset_id { get; set; }
        public int sale_price { get; set; }
        public int purchase_price { get; set; }
        public byte outfit_type { get; set; }
        public DbOutfitSource outfit_source { get; set; }
    }

    public enum DbOutfitSource
    {
        cas,
        rack
    }
}
