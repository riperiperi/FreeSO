using FSO.Common.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Tuning
{
    public class DbTuning : DynTuningEntry
    {
        public DbTuningType owner_type { get; set; }
        public int owner_id { get; set; }
    }

    public enum DbTuningType
    {
        STATIC = 1,
        DYNAMIC = 2,
        EVENT = 3
    }
}
