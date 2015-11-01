using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Lots
{
    public class SqlLots : AbstractSqlDA, ILots
    {
        public SqlLots(ISqlContext context) : base(context){
        }

        public DbLot Get(uint id){
            return Context.Connection.Query<DbLot>("SELECT * FROM fso_lots WHERE lot_id = @id", new { id = id }).FirstOrDefault();
        }

        public uint Create(DbLot lot)
        {
            return (uint)Context.Connection.Query<int>("INSERT INTO fso_lots (shard_id, name, description, " +
                                        "owner_id, location, neighborhood_id, created_date, category_change_date, category, buildable_area) " +
                                        " VALUES (@shard_id, @name, @description, @owner_id, @location, " +
                                        " @neighborhood_id, @created_date, @category_change_date, @category, @buildable_area); SELECT LAST_INSERT_ID();", new
                                        {
                                            shard_id = lot.shard_id,
                                            name = lot.name,
                                            description = lot.description,
                                            owner_id = lot.owner_id,
                                            location = lot.location,
                                            neighborhood_id = lot.neighborhood_id,
                                            created_date = lot.created_date,
                                            category_change_date = lot.category_change_date,
                                            category = lot.category.ToString(),
                                            buildable_area = lot.buildable_area
                                        }).First();
        }

        public DbLot GetByOwner(uint owner_id)
        {
            return Context.Connection.Query<DbLot>("SELECT * FROM fso_lots WHERE owner_id = @id", new { id = owner_id }).FirstOrDefault();
        }
    }
}
