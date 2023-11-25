using Dapper;
using FSO.Server.Database.DA.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Server.Database.DA.Objects
{
    public class SqlObjects : AbstractSqlDA, IObjects
    {
        public SqlObjects(ISqlContext context) : base(context){
        }

        public IEnumerable<DbObject> All(int shard_id)
        {
            return Context.Connection.Query<DbObject>("SELECT * FROM fso_objects WHERE shard_id = @shard_id", new { shard_id = shard_id });
        }

        public uint Create(DbObject obj)
        {
            return (uint)Context.Connection.Query<int>("INSERT INTO fso_objects (shard_id, owner_id, lot_id, " +
                                        "dyn_obj_name, type, graphic, value, budget, upgrade_level, has_db_attributes) " +
                                        " VALUES (@shard_id, @owner_id, @lot_id, @dyn_obj_name, @type," +
                                        " @graphic, @value, @budget, @upgrade_level, @has_db_attributes); SELECT LAST_INSERT_ID();"
                                        , obj).First();
        }

        public DbObject Get(uint id)
        {
            return Context.Connection.Query<DbObject>("SELECT * FROM fso_objects WHERE object_id = @object_id", new { object_id = id }).FirstOrDefault();
        }

        public bool Delete(uint id)
        {
            return Context.Connection.Execute("DELETE FROM fso_objects WHERE object_id = @object_id", new { object_id = id }) > 0;
        }

        public List<DbObject> GetAvatarInventory(uint avatar_id)
        {
            return Context.Connection.Query<DbObject>("SELECT * FROM fso_objects WHERE owner_id = @avatar_id AND lot_id IS NULL", new { avatar_id = avatar_id }).ToList();
        }

        public List<DbObject> GetAvatarInventoryWithAttrs(uint avatar_id)
        {
            var inventory = GetAvatarInventory(avatar_id);
            var toLoad = inventory.Where(x => x.has_db_attributes > 0);
            if (toLoad.Count() == 0) return inventory;
            var attrs = GetObjectAttributes(toLoad.Select(x => x.object_id).ToList());
            foreach (var attr in attrs)
            {
                var target = toLoad.FirstOrDefault(x => x.object_id == attr.object_id);
                if (target == null) continue;
                if (target.AugmentedAttributes == null) target.AugmentedAttributes = new List<int>();
                var targList = target.AugmentedAttributes;
                while (targList.Count <= attr.index) targList.Add(0);
                targList[attr.index] = attr.value;
            }

            return inventory;
        }

        public List<DbObject> ObjOfTypeForAvatar(uint avatar_id, uint guid)
        {
            return Context.Connection.Query<DbObject>("SELECT * FROM fso_objects WHERE owner_id = @avatar_id AND type = @guid",
                new { avatar_id = avatar_id, guid = guid }).ToList();
        }

        public List<DbObject> ObjOfTypeInAvatarInventory(uint avatar_id, uint guid)
        {
            return Context.Connection.Query<DbObject>("SELECT * FROM fso_objects WHERE owner_id = @avatar_id AND lot_id IS NULL AND type = @guid", 
                new { avatar_id = avatar_id, guid = guid}).ToList();
        }

        public int ReturnLostObjects(uint lot_id, IEnumerable<uint> object_ids)
        {
            var sCommand = new StringBuilder();
            bool first = true;
            foreach (var item in object_ids)
            {
                if (first) sCommand.Append("(");
                else sCommand.Append(",");
                sCommand.Append(item);
                first = false;
            }
            sCommand.Append(")");

            return Context.Connection.Execute("UPDATE fso_objects SET lot_id = NULL WHERE lot_id = @lot_id AND object_id NOT IN " + sCommand.ToString(), new { lot_id = lot_id });
        }

        public List<DbObject> GetObjectOwners(IEnumerable<uint> object_ids)
        {
            if (object_ids.Count() == 0) return new List<DbObject>();
            var sCommand = new StringBuilder();
            bool first = true;
            foreach (var item in object_ids)
            {
                if (first) sCommand.Append("(");
                else sCommand.Append(",");
                sCommand.Append(item);
                first = false;
            }
            sCommand.Append(")");

            return Context.Connection.Query<DbObject>("SELECT object_id, lot_id, owner_id FROM fso_objects WHERE object_id IN " + sCommand.ToString()).ToList();
        }

        public bool ConsumeObjsOfTypeInAvatarInventory(uint avatar_id, uint guid, int num)
        {
            var objs = ObjOfTypeInAvatarInventory(avatar_id, guid);
            if (objs.Count < num) return false;
            //perform transaction to remove correct number of items from inventory.
            var t = Context.Connection.BeginTransaction();
            try
            {
                var sel = new List<DbObject>();
                for (int i = 0; i < num; i++) sel.Add(objs[i]);
                var sCommand = new StringBuilder();
                bool first = true;
                foreach (var item in sel)
                {
                    if (first) sCommand.Append("(");
                    else sCommand.Append(",");
                    sCommand.Append(item.object_id);
                    first = false;
                }
                sCommand.Append(");");
                var deleted = Context.Connection.Execute("DELETE FROM fso_objects WHERE object_id IN "+sCommand.ToString());
                if (deleted != num) throw new Exception("Inventory modified while attempting to delete objects!");
            } catch (Exception)
            {
                t.Rollback();
                return false;
            }
            t.Commit();
            return true;
        }

        public List<DbObject> GetByAvatarId(uint avatar_id)
        {
            return Context.Connection.Query<DbObject>("SELECT * FROM fso_objects WHERE owner_id = @avatar_id", new { avatar_id = avatar_id }).ToList();
        }

        public List<DbObject> GetByAvatarIdLot(uint avatar_id, uint lot_id)
        {
            return Context.Connection.Query<DbObject>("SELECT * FROM fso_objects WHERE owner_id = @avatar_id AND lot_id = @lot_id", new { avatar_id = avatar_id, lot_id = lot_id }).ToList();
        }

        public int UpdateObjectOwnerLot(uint avatar_id, int lot_id, uint targ_avatar_id, List<uint> untradableGUIDs)
        {
            var sCommand = new StringBuilder();
            bool first = true;
            foreach (var item in untradableGUIDs)
            {
                if (first) sCommand.Append("(");
                else sCommand.Append(",");
                sCommand.Append(item);
                first = false;
            }
            sCommand.Append(")");

            return Context.Connection.Execute("UPDATE fso_objects SET owner_id = @targ_avatar_id WHERE lot_id = @lot_id AND owner_id = @avatar_id AND type NOT IN " + sCommand.ToString(),
                new { avatar_id = avatar_id, lot_id = lot_id, targ_avatar_id = targ_avatar_id });
        }

        public bool SetInLot(uint id, uint? lot_id)
        {
            return Context.Connection.Execute("UPDATE fso_objects SET lot_id = @lot_id WHERE object_id = @object_id AND ((@lot_id IS NULL) OR (lot_id IS NULL))", new { lot_id = lot_id, object_id = id }) > 0;
        }

        public bool UpdatePersistState(uint id, DbObject obj)
        {
            return Context.Connection.Execute("UPDATE fso_objects " 
                +"SET lot_id = @lot_id, "
                + "owner_id = @owner_id, "
                + "dyn_obj_name = @dyn_obj_name, "
                + "graphic = @graphic, "
                + "value = @value, "
                + "dyn_flags_1 = @dyn_flags_1, "
                + "dyn_flags_2 = @dyn_flags_2, "
                + "upgrade_level = @upgrade_level, "
                + "has_db_attributes = @has_db_attributes "
                + "WHERE object_id = @object_id AND (@lot_id IS NULL OR lot_id = @lot_id);", obj) > 0;
        }

        public int ChangeInventoryOwners(IEnumerable<uint> object_ids, uint oldOwner, uint newOwner)
        {
            if (object_ids.Count() == 0) return 0;
            var sCommand = new StringBuilder();
            bool first = true;
            foreach (var item in object_ids)
            {
                if (first) sCommand.Append("(");
                else sCommand.Append(",");
                sCommand.Append(item);
                first = false;
            }
            sCommand.Append(")");

            return Context.Connection.Execute("UPDATE fso_objects SET owner_id = @newOwner WHERE owner_id = @oldOwner AND object_id IN " + sCommand.ToString(), new { oldOwner = oldOwner, newOwner = newOwner });
        }

        public List<DbObjectAttribute> GetObjectAttributes(List<uint> objects)
        {
            return Context.Connection.Query<DbObjectAttribute>("SELECT * FROM fso_object_attributes WHERE object_id in @ids", new { ids = objects }).ToList();
        }

        public int GetSpecificObjectAttribute(uint objectID, int index)
        {
            return Context.Connection.Query<int>("SELECT value FROM fso_object_attributes WHERE object_id = @object_id AND `index` = @index", new { object_id = objectID, index }).FirstOrDefault();
        }

        public void SetObjectAttributes(List<DbObjectAttribute> attrs)
        {
            Context.Connection.ExecuteBufferedInsert("INSERT INTO fso_object_attributes (object_id, `index`, value) VALUES (@object_id, @index, @value) ON DUPLICATE KEY UPDATE value = @value", attrs, 100);
        }

        public int TotalObjectAttributes(uint guid, int index)
        {
            return Context.Connection.Query<int>("SELECT SUM(a.value) " +
                "FROM fso_object_attributes a JOIN fso_objects o ON a.object_id = o.object_id " +
                "WHERE `type` = @guid AND `index` = @index", new { guid, index }).FirstOrDefault();
        }
    }
}
