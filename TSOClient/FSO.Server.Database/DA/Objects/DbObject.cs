using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Objects
{
    public class DbObject
    {
        //general persist idenfification
        public uint object_id { get; set; }
        public int shard_id { get; set; }
        public uint? owner_id { get; set; }
        public int? lot_id { get; set; }

        //object info. most of this is unused, but can be used to show state for inventory objects
        public string dyn_obj_name { get; set; }
        public uint type { get; set; } //guid
        public ushort graphic { get; set; }
        public uint value { get; set; }
        public int budget { get; set; }
        public ulong dyn_flags_1 { get; set; }
        public ulong dyn_flags_2 { get; set; }
        
        //upgrades
        public uint upgrade_level { get; set; }

        //token system
        //token objects cannot be traded or placed on a lot, but you can still see them in your inventory. 
        //they are managed by object scripts.
        public bool isToken { get; set; } 
        public int tokenCount { get; set; } //how many of this token there are in this stack
        public uint tokenType { get; set; } //a GUID this token points to. used by tokens that point to other object types, such as car keys
    }
}
