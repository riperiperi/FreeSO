using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;

namespace FSO.Server.Database.DA.AuthTickets
{
    public class SqlAuthTickets : AbstractSqlDA, IAuthTickets
    {
        public SqlAuthTickets(ISqlContext context) : base(context)
        {
        }

        public void Create(AuthTicket ticket)
        {
            Context.Connection.Execute("INSERT INTO fso_auth_tickets VALUES (@ticket_id, @user_id, @date, @ip)", ticket);
        }
    }
}
