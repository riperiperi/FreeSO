using System.Collections.Generic;

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
        //if >0, attributes are stored on db rather than in state.
        //if 2, we're a value token. (attr 0 contains number that should be displayed in UI)
        public byte has_db_attributes { get; set; }

        public List<int> AugmentedAttributes;
    }

    public class DbObjectCreate
    {
        public uint object_id { get; set; }
        public int shard_id { get; set; }
        public uint? owner_id { get; set; }
        public int? lot_id { get; set; }
        public string dyn_obj_name { get; set; }
        public ulong type { get; set; } //guid: when creating this is a ulong to work around sqlite bugs
        public ushort graphic { get; set; }
        public uint value { get; set; }
        public int budget { get; set; }
        public ulong dyn_flags_1 { get; set; }
        public ulong dyn_flags_2 { get; set; }
        public uint upgrade_level { get; set; }
        public byte has_db_attributes { get; set; }

        public DbObjectCreate(DbObject obj)
        {
            object_id = obj.object_id;
            shard_id = obj.shard_id;
            owner_id = obj.owner_id;
            lot_id = obj.lot_id;
            dyn_obj_name = obj.dyn_obj_name;
            type = obj.type;
            graphic = obj.graphic;
            value = obj.graphic;
            budget = obj.budget;
            dyn_flags_1 = obj.dyn_flags_1;
            dyn_flags_2 = obj.dyn_flags_2;
            upgrade_level = obj.upgrade_level;
            has_db_attributes = obj.has_db_attributes;
        }
    }
}
