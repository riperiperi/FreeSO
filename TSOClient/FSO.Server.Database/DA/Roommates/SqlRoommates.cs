using Dapper;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;

namespace FSO.Server.Database.DA.Roommates
{
    public class SqlRoommates : AbstractSqlDA, IRoommates
    {
        public SqlRoommates(ISqlContext context) : base(context)
        {
        }

        public bool Create(DbRoommate roomie)
        {
            try
            {
                return (uint)Context.Connection.Execute("INSERT INTO fso_roommates (avatar_id, lot_id, permissions_level, is_pending) " +
                                " VALUES (@avatar_id, @lot_id, @permissions_level, @is_pending);", roomie) > 0;
            } catch (SqlException)
            {
                return false;
            }
        }

        public bool CreateOrUpdate(DbRoommate roomie)
        {
            try
            {
                return (uint)Context.Connection.Execute("INSERT INTO fso_roommates (avatar_id, lot_id, permissions_level, is_pending) " +
                                "VALUES (@avatar_id, @lot_id, @permissions_level, @is_pending) " +
                                "ON DUPLICATE KEY UPDATE permissions_level = @permissions_level, is_pending = 0", roomie) > 0;
            }
            catch (SqlException)
            {
                return false;
            }
        }

        public DbRoommate Get(uint avatar_id, int lot_id)
        {
            return Context.Connection.Query<DbRoommate>("SELECT * FROM fso_roommates WHERE avatar_id = @avatar_id AND lot_id = @lot_id", 
                new { avatar_id = avatar_id, lot_id = lot_id }).FirstOrDefault();
        }
        public List<DbRoommate> GetAvatarsLots(uint avatar_id)
        {
            return Context.Connection.Query<DbRoommate>("SELECT * FROM fso_roommates WHERE avatar_id = @avatar_id",
                new { avatar_id = avatar_id }).ToList();
        }
        public List<DbRoommate> GetLotRoommates(int lot_id)
        {
            return Context.Connection.Query<DbRoommate>("SELECT * FROM fso_roommates WHERE lot_id = @lot_id",
                new { lot_id = lot_id }).ToList();
        }
        public uint RemoveRoommate(uint avatar_id, int lot_id)
        {
            return (uint)Context.Connection.Execute("DELETE FROM fso_roommates WHERE avatar_id = @avatar_id AND lot_id = @lot_id",
                new { avatar_id = avatar_id, lot_id = lot_id });
        }

        public bool DeclineRoommateRequest(uint avatar_id, int lot_id)
        {
            return Context.Connection.Execute("DELETE FROM fso_roommates WHERE avatar_id = @avatar_id AND lot_id = @lot_id AND is_pending = 1",
                new { avatar_id = avatar_id, lot_id = lot_id }) > 0;
        }
        public bool AcceptRoommateRequest(uint avatar_id, int lot_id)
        {
            return Context.Connection.Execute("UPDATE fso_roommates SET is_pending = 0 WHERE avatar_id = @avatar_id AND lot_id = @lot_id AND is_pending = 1", 
                new { avatar_id = avatar_id, lot_id = lot_id }) > 0;
        }
        public bool UpdatePermissionsLevel(uint avatar_id, int lot_id, byte level)
        {
            return Context.Connection.Execute("UPDATE fso_roommates SET permissions_level = @level WHERE avatar_id = @avatar_id AND lot_id = @lot_id",
                new { level = level, avatar_id = avatar_id, lot_id = lot_id }) > 0;
        }
    }
}
