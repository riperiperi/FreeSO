using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FSO.Server.Database.DA.LotVisitors
{
    public class SqlLotVisits : AbstractSqlDA, ILotVisits
    {
        public SqlLotVisits(ISqlContext context) : base(context){
        }

        public int? Visit(uint avatar_id, DbLotVisitorType visitor_type, int lot_id)
        {
            try {
                if (Context.SupportsFunctions)
                {
                    //Stored procedure will handle erroring any active visits that should no longer be active
                    return Context.Connection.Query<int>("SELECT `fso_lot_visits_create`(@avatar_id, @lot_id, @type)", new
                    {
                        avatar_id = avatar_id,
                        lot_id = lot_id,
                        type = visitor_type.ToString()
                    }).First();
                }
                else
                {
                    /* 
CREATE FUNCTION `fso_lot_visits_create`(`p_avatar_id` INT, `p_lot_id` INT, `p_visitor_type` VARCHAR(50)) RETURNS int(11)
    READS SQL DATA
BEGIN
	#Error any open active visit, can only have one active
	UPDATE fso_lot_visits SET `status` = 'failed', time_closed = current_timestamp WHERE avatar_id = p_avatar_id AND `status` = 'active';
	#Record visit
	INSERT INTO fso_lot_visits (avatar_id, lot_id, type, status) VALUES (p_avatar_id, p_lot_id, p_visitor_type, 'active'); 
	RETURN LAST_INSERT_ID();
END;*/
                    return Context.Connection.Query<int>(Context.CompatLayer(@"UPDATE fso_lot_visits SET `status` = 'failed', time_closed = current_timestamp WHERE avatar_id = @avatar_id AND `status` = 'active';
	INSERT INTO fso_lot_visits (avatar_id, lot_id, type, status) VALUES (@avatar_id, @lot_id, @type, 'active'); 
	SELECT LAST_INSERT_ID();"), new {
                        avatar_id = avatar_id,
                        lot_id = lot_id,
                        type = visitor_type.ToString()
                    }).First();

                }
            }catch(Exception ex){
                return null;
            }
        }

        public void Leave(int visit_id)
        {
            try
            {
                Context.Connection.Query("UPDATE `fso_lot_visits` SET status = 'closed', time_closed = current_timestamp WHERE lot_visit_id = @visit_id AND `status` = 'active'", new { visit_id = visit_id });
            }catch(Exception ex){
            }
        }

        public void Renew(IEnumerable<int> visit_ids)
        {
            try{
                Context.Connection.Query("UPDATE `fso_lot_visits` SET time_closed = current_timestamp WHERE lot_visit_id IN @visit_ids AND `status` = 'active'", new { visit_ids = visit_ids });
            }catch (Exception ex){
            }
        }

        public void PurgeByDate(DateTime date)
        {
            Context.Connection.Query("DELETE FROM `fso_lot_visits` WHERE time_closed IS NOT NULL AND time_closed < @date", new { date = date });
            Context.Connection.Query("DELETE FROM `fso_lot_visits` WHERE time_closed IS NULL AND time_created < @date", new { date = date });
        }
        
        public IEnumerable<DbLotVisit> StreamBetween(int shard_id, DateTime start, DateTime end)
        {
            return Context.Connection.Query<DbLotVisit>(
                "SELECT * FROM `fso_lot_visits` v INNER JOIN fso_lots l ON v.lot_id = l.lot_id " +
                    "WHERE l.shard_id = @shard_id AND status != 'failed' " + 
                    "AND time_closed IS NOT NULL " + 
                    "AND type = 'visitor' " +
                    "AND (time_created BETWEEN @start AND @end OR time_closed BETWEEN @start and @end)",
                new { start = start, end = end, shard_id = shard_id }, buffered: false);
        }

        public IEnumerable<DbLotVisitNhood> StreamBetweenPlusNhood(int shard_id, DateTime start, DateTime end)
        {
            return Context.Connection.Query<DbLotVisitNhood>(
                "SELECT * FROM `fso_lot_visits` v INNER JOIN fso_lots l ON v.lot_id = l.lot_id " +
                    "WHERE l.shard_id = @shard_id AND status != 'failed' " +
                    "AND time_closed IS NOT NULL " +
                    "AND (time_created BETWEEN @start AND @end OR time_closed BETWEEN @start and @end)",
                new { start = start, end = end, shard_id = shard_id }, buffered: false);
        }

        public IEnumerable<DbLotVisitNhood> StreamBetweenOneNhood(uint neighborhood_id, DateTime start, DateTime end)
        {
            return Context.Connection.Query<DbLotVisitNhood>(
                "SELECT * FROM `fso_lot_visits` v INNER JOIN fso_lots l ON v.lot_id = l.lot_id " +
                    "WHERE l.neighborhood_id = @neighborhood_id AND status != 'failed' " +
                    "AND time_closed IS NOT NULL " +
                    "AND type = 'visitor' " +
                    "AND (time_created BETWEEN @start AND @end OR time_closed BETWEEN @start and @end)",
                new { start = start, end = end, neighborhood_id = neighborhood_id }, buffered: false);
        }
    }
}
