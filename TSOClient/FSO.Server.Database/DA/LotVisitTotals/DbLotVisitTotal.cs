using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.LotVisitTotals
{
    public class DbLotVisitTotal
    {
        public int lot_id { get; set; }
        public DateTime date { get; set; }
        public int minutes { get; set; }
    }
}
