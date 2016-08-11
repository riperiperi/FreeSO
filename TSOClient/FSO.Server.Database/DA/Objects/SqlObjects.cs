using Dapper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                                        "dyn_obj_name, type, graphic, value, budget) " +
                                        " VALUES (@shard_id, @owner_id, @lot_id, @dyn_obj_name, @type," +
                                        " @graphic, @value, @budget); SELECT LAST_INSERT_ID();"
                                        , obj).First();
        }

        public DbObject Get(uint id)
        {
            return Context.Connection.Query<DbObject>("SELECT * FROM fso_objects WHERE object_id = @object_id", new { object_id = id }).FirstOrDefault();
        }

        public List<DbObject> GetAvatarInventory(uint avatar_id)
        {
            return Context.Connection.Query<DbObject>("SELECT * FROM fso_objects WHERE owner_id = @avatar_id AND lot_id IS NULL", new { avatar_id = avatar_id }).ToList();
        }

        public List<DbObject> GetByAvatarId(uint avatar_id)
        {
            return Context.Connection.Query<DbObject>("SELECT * FROM fso_objects WHERE owner_id = @avatar_id", new { avatar_id = avatar_id }).ToList();
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
                + "dyn_flags_2 = @dyn_flags_2 "
                + "WHERE object_id = @object_id AND (@lot_id IS NULL OR lot_id = @lot_id);", obj) > 0;
        }
    }
}
