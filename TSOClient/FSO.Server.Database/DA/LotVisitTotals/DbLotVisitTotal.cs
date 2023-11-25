using System;

namespace FSO.Server.Database.DA.LotVisitTotals
{
    public class DbLotVisitTotal
    {
        public int lot_id { get; set; }
        public DateTime date { get; set; }
        public int minutes { get; set; }
    }
}
