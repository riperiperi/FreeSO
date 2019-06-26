using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Elections
{
    public class DbElectionCycleMail
    {
        public uint avatar_id { get; set; }
        public uint cycle_id { get; set; }
        public DbElectionCycleState cycle_state { get; set; }
    }
}
