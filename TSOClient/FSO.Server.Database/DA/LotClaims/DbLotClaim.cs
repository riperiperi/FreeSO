namespace FSO.Server.Database.DA.LotClaims
{
    public class DbLotClaim
    {
        public int claim_id { get; set; }
        public int shard_id { get; set; }
        public int lot_id { get; set; }
        public string owner { get; set; }
    }

    public class DbLotStatus
    {
        public uint location { get; set; }
        public int active { get; set; }
    }

    public class DbLotActive
    {
        public int lot_id { get; set; }
        public int shard_id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public uint location { get; set; }
        public uint neighborhood_id { get; set; }
        public uint admit_mode { get; set; }
        public FSO.Common.Enum.LotCategory category { get; set; }
        public int active { get; set; }
    }
}
