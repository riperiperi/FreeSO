using Dapper;
using FSO.Server.Common;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Server.Database.DA.Neighborhoods
{
    public class SqlNeighborhoods : AbstractSqlDA, INeighborhoods
    {
        public SqlNeighborhoods(ISqlContext context) : base(context)
        {
        }

        public int AddNhood(DbNeighborhood hood)
        {
            var result = Context.Connection.Query<int>("INSERT INTO fso_neighborhoods (name, description, " +
                        "shard_id, location, color, guid) " +
                        " VALUES (@name, @description, " +
                        " @shard_id, @location, @color, @guid); SELECT LAST_INSERT_ID();", hood).First();
            return result;
        }

        public int DeleteMissing(int shard_id, List<string> AllGUIDs)
        {
            var sCommand = new StringBuilder();
            bool first = true;
            foreach (var item in AllGUIDs)
            {
                if (first) sCommand.Append("(");
                else sCommand.Append(",");
                sCommand.Append("'"+item+"'");
                first = false;
            }
            sCommand.Append(")");

            var deleted = Context.Connection.Execute("DELETE FROM fso_neighborhoods WHERE shard_id = @shard_id AND guid NOT IN " 
                + sCommand.ToString(), new { shard_id = shard_id });

            return deleted;
        }

        public List<DbNeighborhood> All(int shard_id)
        {
            return Context.Connection.Query<DbNeighborhood>("SELECT * FROM fso_neighborhoods WHERE shard_id = @shard_id", 
                new { shard_id = shard_id }).ToList();
        }

        public DbNeighborhood Get(uint neighborhood_id)
        {
            return Context.Connection.Query<DbNeighborhood>("SELECT * FROM fso_neighborhoods WHERE neighborhood_id = @neighborhood_id",
                new { neighborhood_id = neighborhood_id}).FirstOrDefault();
        }

        public DbNeighborhood GetByMayor(uint avatar_id)
        {
            return Context.Connection.Query<DbNeighborhood>("SELECT * FROM fso_neighborhoods WHERE mayor_id = @avatar_id",
                new { avatar_id = avatar_id }).FirstOrDefault();
        }

        public DbNeighborhood GetByLocation(uint location)
        {
            return Context.Connection.Query<DbNeighborhood>(
                "SELECT neighborhood_id " +
                "FROM fso.fso_neighborhoods n " +
                "ORDER BY(POWER(((@location & 65535) + 0.0) - ((n.location & 65535) + 0.0), 2) + " +
                "POWER((FLOOR(@location / 65536) + 0.0) - (FLOOR(n.location / 65536) + 0.0), 2)) " +
                "LIMIT 1", new { location = location }).FirstOrDefault();
        }

        public int UpdateFromJSON(DbNeighborhood update)
        {
            if (update.description != null)
                return Context.Connection.Execute("UPDATE fso_neighborhoods SET name = @name, description = @description, "
                    + "location = @location, color = @color WHERE guid = @guid AND shard_id = @shard_id", update);
            else
                return Context.Connection.Execute("UPDATE fso_neighborhoods SET name = @name, location = @location, "
                    + "color = @color WHERE guid = @guid AND shard_id = @shard_id", update);
        }

        public void UpdateDescription(uint neigh_id, string description)
        {
            Context.Connection.Query("UPDATE fso_neighborhoods SET description = @desc WHERE neighborhood_id = @id", new { id = neigh_id, desc = description });
        }

        public void UpdateMayor(uint neigh_id, uint? mayor_id)
        {
            Context.Connection.Query("UPDATE fso_neighborhoods SET mayor_id = @mayor_id, mayor_elected_date = @date WHERE neighborhood_id = @id", 
                new { id = neigh_id, mayor_id = mayor_id, date = Epoch.Now });
        }

        public void UpdateTownHall(uint neigh_id, uint? lot_id)
        {
            Context.Connection.Query("UPDATE fso_neighborhoods SET town_hall_id = @lot_id WHERE neighborhood_id = @id", new { id = neigh_id, lot_id = lot_id });
        }

        public void UpdateCycle(uint neigh_id, uint? cycle_id)
        {
            Context.Connection.Query("UPDATE fso_neighborhoods SET election_cycle_id = @cycle_id WHERE neighborhood_id = @id", new { id = neigh_id, cycle_id = cycle_id });
        }

        public void UpdateName(uint neigh_id, string name)
        {
            Context.Connection.Query("UPDATE fso_neighborhoods SET name = @name WHERE neighborhood_id = @id", new { id = neigh_id, name = name });
        }

        public void UpdateFlag(uint neigh_id, uint flag)
        {
            Context.Connection.Query("UPDATE fso_neighborhoods SET flag = @flag WHERE neighborhood_id = @id", new { id = neigh_id, flag = flag });
        }

        public DbNhoodBan GetNhoodBan(uint user_id)
        {
            var date = Epoch.Now;
            return Context.Connection.Query<DbNhoodBan>("SELECT * FROM fso_nhood_ban WHERE user_id = @user_id AND end_date > @date",
                new { user_id = user_id, date = date }).FirstOrDefault();
        }
        
        public bool AddNhoodBan(DbNhoodBan ban)
        {
            var result = Context.Connection.Query<int>("INSERT INTO fso_nhood_ban (user_id, ban_reason, end_date) " +
                        "VALUES (@user_id, @ban_reason, @end_date) " +
                        "ON DUPLICATE KEY UPDATE ban_reason = @ban_reason, end_date = @end_date; " +
                        "SELECT LAST_INSERT_ID();", ban).First();
            return result > 0;
        }

        public List<DbNeighborhood> SearchExact(int shard_id, string name, int limit)
        {
            return Context.Connection.Query<DbNeighborhood>(
                "SELECT neighborhood_id, location, name FROM fso_neighborhoods WHERE shard_id = @shard_id AND name = @name LIMIT @limit",
                new { shard_id = shard_id, name = name, limit = limit }
            ).ToList();
        }

        public List<DbNeighborhood> SearchWildcard(int shard_id, string name, int limit)
        {
            name = name
                .Replace("!", "!!")
                .Replace("%", "!%")
                .Replace("_", "!_")
                .Replace("[", "!["); //must sanitize format...
            return Context.Connection.Query<DbNeighborhood>(
                "SELECT neighborhood_id, location, name FROM fso_neighborhoods WHERE shard_id = @shard_id AND name LIKE @name LIMIT @limit",
                new { shard_id = shard_id, name = "%" + name + "%", limit = limit }
            ).ToList();
        }

    }
}
