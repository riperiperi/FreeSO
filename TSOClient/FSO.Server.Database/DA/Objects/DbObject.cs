using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Objects
{
    public class DbObject
    {
        public uint object_id { get; set; }
        public int shard_id { get; set; }
        public uint? owner_id { get; set; }
        public int? lot_id { get; set; }
        public string dyn_obj_name { get; set; }
        public uint type { get; set; }
        public ushort graphic { get; set; }
        public uint value { get; set; }
        public int budget { get; set; }
    }
}
