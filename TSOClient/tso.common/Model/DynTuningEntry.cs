using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Common.Model
{
    public class DynTuningEntry
    {
        public string tuning_type { get; set; }
        public int tuning_table { get; set; }
        public int tuning_index { get; set; }
        public float value { get; set; }
    }
}
