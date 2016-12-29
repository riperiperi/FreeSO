using Dapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Database.DA.Bonus
{
    public class SqlBonus : AbstractSqlDA, IBonus
    {
        public SqlBonus(ISqlContext context) : base(context)
        {
        }

        public IEnumerable<DbBonus> GetByAvatarId(uint avatar_id)
        {
            return Context.Connection.Query<DbBonus>("SELECT * FROM fso_bonus WHERE avatar_id = @avatar_id ORDER BY time_issued DESC LIMIT 14", new { avatar_id = avatar_id });
        }
    }
}
