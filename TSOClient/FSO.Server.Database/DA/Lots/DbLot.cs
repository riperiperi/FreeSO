using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Lots
{
    public class DbLot
    {
        public uint lot_id { get; set; }
        public int shard_id { get; set; }

        public string name { get; set; }
        public string description { get; set; }
        public uint location { get; set; }
        public uint neighborhood_id { get; set; }
        public uint created_date { get; set; }
        public uint category_change_date { get; set; }
        public byte category { get; set; }
        public uint buildable_area { get; set; }
    }
}
