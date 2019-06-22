using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Tuning
{
    public class DbTuningPreset
    {
        public int preset_id;
        public string name;
        public string description;
        public int flags;
    }

    public class DbTuningPresetItem
    {
        public int item_id;
        public int preset_id;
        public string tuning_type;
        public int tuning_table;
        public int tuning_index;
        public float value;
    }
}
