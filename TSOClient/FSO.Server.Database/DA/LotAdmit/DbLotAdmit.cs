using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.LotAdmit
{
    public class DbLotAdmit
    {
        public int lot_id { get; set; }
        public uint avatar_id { get; set; }
        public byte admit_type { get; set; }
    }
}
