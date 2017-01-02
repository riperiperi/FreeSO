using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.LotVisitors
{
    public class DbLotVisit
    {
        public int lot_visit_id { get; set; }
        public uint avatar_id { get; set; }
        public int lot_id { get; set; }
        public DbLotVisitorType type { get; set; }
        public DbLotVisitorStatus status { get; set; }
        public DateTime time_created { get; set; }
        public DateTime? time_closed { get; set; }
    }

    public enum DbLotVisitorType
    {
        owner,
        roommate,
        visitor
    }

    public enum DbLotVisitorStatus
    {
        active,
        closed,
        failed
    }
}
