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

        public void UpdatePersistState(uint id, DbObject obj)
        {
            throw new NotImplementedException();
        }
    }
}
