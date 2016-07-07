using Dapper;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Relationships
{
    public class SqlRelationships : AbstractSqlDA, IRelationships
    {

        public SqlRelationships(ISqlContext context) : base(context)
        {

        }
        public int Delete(uint entity_id)
        {
            return Context.Connection.ExecuteScalar<int>(
                "DELETE FROM fso_relationships WHERE from_id = @entity_id OR to_id = @entity_id",
                new { entity_id = entity_id }
            );
        }

        public List<DbRelationship> GetBidirectional(uint entity_id)
        {
            return Context.Connection.Query<DbRelationship>(
                "SELECT * FROM fso_relationships WHERE from_id = @entity_id OR to_id = @entity_id",
                new { entity_id = entity_id }
            ).ToList();
        }

        public List<DbRelationship> GetOutgoing(uint entity_id)
        {
            return Context.Connection.Query<DbRelationship>(
                "SELECT * FROM fso_relationships WHERE from_id = @entity_id",
                new { entity_id = entity_id }
            ).ToList();
        }

        public int UpdateMany(List<DbRelationship> entries)
        {
            var conn = (MySqlConnection)Context.Connection;
            int rows;
            using (MySqlCommand cmd = new MySqlCommand("", conn))
            {
                try
                {
                    StringBuilder sCommand = new StringBuilder("INSERT INTO fso_relationships (from_id, to_id, value, `index`) VALUES ");

                    bool first = true;
                    foreach (var item in entries)
                    {
                        if (!first) sCommand.Append(",");
                        first = false;
                        sCommand.Append("(");
                        sCommand.Append(item.from_id);
                        sCommand.Append(",");
                        sCommand.Append(item.to_id);
                        sCommand.Append(",");
                        sCommand.Append(item.value);
                        sCommand.Append(",");
                        sCommand.Append(item.index);
                        sCommand.Append(")");
                    }
                    sCommand.Append(" ON DUPLICATE KEY UPDATE value = VALUES(`value`); ");

                    cmd.CommandTimeout = 300;
                    cmd.CommandText = sCommand.ToString();
                    rows = cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    return -1;
                }
                return rows;
            }
        }
    }
}
