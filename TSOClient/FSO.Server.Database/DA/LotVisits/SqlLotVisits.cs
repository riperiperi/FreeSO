using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.LotVisitors
{
    public class SqlLotVisits : AbstractSqlDA, ILotVisits
    {
        public SqlLotVisits(ISqlContext context) : base(context){
        }

        public int? Visit(uint avatar_id, DbLotVisitorType visitor_type, int lot_id)
        {
            try {
                //Stored procedure will handle erroring any active visits that should no longer be active
                return Context.Connection.Query<int>("SELECT `fso_lot_visits_create`(@avatar_id, @lot_id, @type)", new {
                        avatar_id = avatar_id,
                        lot_id = lot_id,
                        type = visitor_type.ToString()
                    }).First();
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
    }
}
