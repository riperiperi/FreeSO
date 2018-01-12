using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.DynPayouts
{
    public class DbTransSummary
    {
        public int transaction_type { get; set; }
        public int value { get; set; }
        public int sum { get; set; }
    }
}
