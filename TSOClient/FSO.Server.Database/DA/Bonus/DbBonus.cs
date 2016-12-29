using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Bonus
{
    public class DbBonus
    {
        public int bonus_id { get; set; }
        public uint avatar_id { get; set; }
        public DateTime time_issued { get; set; }
        public int bonus_visitor { get; set; }
        public int bonus_property { get; set; }
        public int bonus_sim { get; set; }
    }
}
