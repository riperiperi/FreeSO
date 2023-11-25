using FSO.Common.Enum;

namespace FSO.Server.Database.DA.Bonus
{
    public class DbBonusMetrics
    {
        public uint avatar_id { get; set; }
        public int lot_id { get; set; }
        public LotCategory category { get; set; }
        public int? visitor_minutes { get; set; }
        public byte? property_rank { get; set; }
        public byte? sim_rank { get; set; }
    }
}
