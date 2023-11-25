using System;

namespace FSO.Server.Database.DA.Bonus
{
    public class DbBonus
    {
        public int bonus_id { get; set; }
        public uint avatar_id { get; set; }
        public DateTime period { get; set; }
        public int? bonus_visitor { get; set; }
        public int? bonus_property { get; set; }
        public int? bonus_sim { get; set; }
    }
}
