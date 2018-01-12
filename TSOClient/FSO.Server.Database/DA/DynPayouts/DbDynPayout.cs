using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.DynPayouts
{
    public class DbDynPayout
    {
        public int day { get; set; }
        public int skilltype { get; set; }
        public float multiplier { get; set; }
        public int flags { get; set; }
    }
}
