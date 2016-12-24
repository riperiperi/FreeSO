using FSO.Server.Database.DA.Lots;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.LotTop100
{
    public class DbLotTop100
    {
        public DbLotCategory category { get; set; }
        public byte rank { get; set; }
        public int shard_id { get; set; }
        public int? lot_id { get; set; }
        public int? minutes { get; set; }
        public DateTime date { get; set; }
    }
}
