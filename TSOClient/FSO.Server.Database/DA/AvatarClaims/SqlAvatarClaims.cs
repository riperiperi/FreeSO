using Dapper;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Server.Database.DA.AvatarClaims
{
    public class SqlAvatarClaims : AbstractSqlDA, IAvatarClaims
    {
        public SqlAvatarClaims(ISqlContext context) : base(context)
        {
        }

        public bool Claim(int id, string previousOwner, string newOwner, uint location)
        {
            try
            {
                Context.Connection.Query("UPDATE fso_avatar_claims SET owner = @owner, location = @location WHERE avatar_claim_id = @claim_id AND owner = @previous_owner", new { claim_id = (int)id, previous_owner = previousOwner, owner = newOwner, location = location });
                var newClaim = Context.Connection.Query<DbAvatarClaim>("SELECT * FROM fso_avatar_claims WHERE avatar_claim_id = @claim_id AND owner = @owner", new { claim_id = (int)id, owner = newOwner }).FirstOrDefault();
                return newClaim != null;
            }
            catch (MySqlException ex)
            {
                return false;
            }
        }

        public void RemoveRemaining(string previousOwner, uint location)
        {
            Context.Connection.Query("DELETE FROM fso_avatar_claims WHERE location = @location AND owner = @previous_owner", new { previous_owner = previousOwner, location = location });
        }


        public void Delete(int id, string owner)
        {
            Context.Connection.Query("DELETE FROM fso_avatar_claims WHERE owner = @owner AND avatar_claim_id = @claim_id", new { owner = owner, claim_id = (int)id });
        }

        public void DeleteAll(string owner)
        {
            Context.Connection.Query("DELETE FROM fso_avatar_claims WHERE owner = @owner", new { owner = owner });
        }

        public DbAvatarClaim Get(int id)
        {
            return Context.Connection.Query<DbAvatarClaim>("SELECT * FROM fso_avatar_claims WHERE avatar_claim_id = @claim_id", new { claim_id = (int)id }).FirstOrDefault();
        }
        public IEnumerable<DbAvatarClaim> GetAll()
        {
            return Context.Connection.Query<DbAvatarClaim>("SELECT * FROM fso_avatar_claims");
        }
        public IEnumerable<DbAvatarActive> GetAllActiveAvatars()
        {
            return Context.Connection.Query<DbAvatarActive>("SELECT b.*, a.location FROM fso.fso_avatar_claims as a "+
                "inner join fso.fso_avatars as b ON a.avatar_id = b.avatar_id;");
        }
        public int? GetAllActiveAvatarsCount()
        {
            return Context.Connection.Query<int>("SELECT COUNT(*) FROM fso_avatar_claims").FirstOrDefault();
        }
        public DbAvatarClaim GetByAvatarID(uint id)
        {
            return Context.Connection.Query<DbAvatarClaim>("SELECT * FROM fso_avatar_claims WHERE avatar_id = @id", new { id = id }).FirstOrDefault();
        }

        public IEnumerable<DbAvatarClaim> GetAllByOwner(string owner)
        {
            return Context.Connection.Query<DbAvatarClaim>("SELECT * FROM fso_avatar_claims WHERE owner = @owner", new { owner = owner });
        }

        public int? TryCreate(DbAvatarClaim claim)
        {
            try
            {
                return Context.Connection.Query<int>("INSERT INTO fso_avatar_claims (avatar_id, owner, location) " +
                    " VALUES (@avatar_id, @owner, @location); SELECT LAST_INSERT_ID();", claim).First();
            }
            catch (MySqlException ex)
            {
                return null;
            }
        }
    }
}
