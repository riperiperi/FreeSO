using Dapper;
using FSO.Common.Enum;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.LotClaims
{
    public class SqlLotClaims : AbstractSqlDA, ILotClaims
    {
        public SqlLotClaims(ISqlContext context) : base(context){
        }

        public bool Claim(uint id, string previousOwner, string newOwner)
        {
            try
            {
                Context.Connection.Query("UPDATE fso_lot_claims SET owner = @owner WHERE claim_id = @claim_id AND owner = @previous_owner", new { claim_id = (int)id, previous_owner = previousOwner, owner = newOwner });
                var newClaim = Context.Connection.Query<DbLotClaim>("SELECT * FROM fso_lot_claims WHERE claim_id = @claim_id AND owner = @owner", new { claim_id = (int)id, owner = newOwner }).FirstOrDefault();
                return newClaim != null;
            }
            catch (MySqlException ex)
            {
                return false;
            }
        }

        public void Delete(uint id, string owner)
        {
            Context.Connection.Query("DELETE FROM fso_lot_claims WHERE owner = @owner AND claim_id = @claim_id", new { owner = owner, claim_id = (int)id });
        }

        public DbLotClaim Get(uint id)
        {
            return Context.Connection.Query<DbLotClaim>("SELECT * FROM fso_lot_claims WHERE claim_id = @claim_id", new { claim_id = (int)id }).FirstOrDefault();
        }

        public DbLotClaim GetByLotID(int id)
        {
            return Context.Connection.Query<DbLotClaim>("SELECT * FROM fso_lot_claims WHERE lot_id = @lot_id", new { lot_id = id }).FirstOrDefault();
        }

        public IEnumerable<DbLotClaim> GetAllByOwner(string owner)
        {
            return Context.Connection.Query<DbLotClaim>("SELECT * FROM fso_lot_claims WHERE owner = @owner", new { owner = owner });
        }

        public void RemoveAllByOwner(string owner)
        {
            Context.Connection.Query("DELETE FROM fso_lot_claims WHERE owner = @owner", new { owner = owner });
        }

        public uint? TryCreate(DbLotClaim claim){

            try {
                return (uint)Context.Connection.Query<int>("INSERT INTO fso_lot_claims (shard_id, lot_id, owner) " +
                    " VALUES (@shard_id, @lot_id, @owner); SELECT LAST_INSERT_ID();", claim).First();
            }catch(MySqlException ex){
                return null;
            }
        }

        public List<DbLotStatus> AllLocations(int shard_id)
        {
            return Context.Connection.Query<DbLotStatus>("SELECT b.location AS location, active " +
            "FROM fso.fso_lot_claims AS a " +
            "JOIN fso.fso_lots AS b " +
            "ON a.lot_id = b.lot_id " +
            "JOIN(SELECT location, COUNT(*) as active FROM fso_avatar_claims GROUP BY location) AS c " +
            "ON b.location = c.location WHERE a.shard_id = @shard_id", new { shard_id = shard_id }).ToList();
        }

        public List<DbLotStatus> Top100Filter(int shard_id, LotCategory category, int limit)
        {
            return Context.Connection.Query<DbLotStatus>("SELECT b.location AS location, active " +
            "FROM fso.fso_lot_claims AS a " +
            "JOIN fso.fso_lots AS b " +
            "ON a.lot_id = b.lot_id " +
            "JOIN(SELECT location, COUNT(*) as active FROM fso_avatar_claims GROUP BY location) AS c " +
            "ON b.location = c.location WHERE a.shard_id = @shard_id " +
            "AND category = @category AND active > 0 " +
            "ORDER BY active DESC " +
            "LIMIT @limit", new { shard_id = shard_id, category = category.ToString(), limit = limit }).ToList();
        }
    }
}
