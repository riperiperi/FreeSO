using System.Linq;
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

        public void Delete(string id)
        {
            Context.Connection.Execute("DELETE FROM fso_auth_tickets WHERE ticket_id = @ticket_id", new { ticket_id = id });
        }

        public AuthTicket Get(string id)
        {
            return 
                Context.Connection.Query<AuthTicket>("SELECT * FROM fso_auth_tickets WHERE ticket_id = @ticket_id", new { ticket_id = id }).FirstOrDefault();
        }

        public void Purge(uint time)
        {
            Context.Connection.Execute("DELETE FROM fso_auth_tickets WHERE date < @time", new { time = time });
        }
    }
}
