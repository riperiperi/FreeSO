using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
