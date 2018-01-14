using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using FSO.Server.Database.DA.Utils;

namespace FSO.Server.Database.DA.EmailConfirmation
{
    public class SqlEmailConfirmations : AbstractSqlDA, IEmailConfirmations
    {
        public SqlEmailConfirmations(ISqlContext context) : base(context)
        {

        }

        public EmailConfirmation GetByToken(string token)
        {
            return Context.Connection.Query<EmailConfirmation>("SELECT * FROM fso_email_confirm WHERE token = @token", new { token = token }).FirstOrDefault();
        }

        public EmailConfirmation GetByEmail(string token)
        {
            return Context.Connection.Query<EmailConfirmation>("SELECT * FROM fso_email_confirm WHERE token = @token", new { token = token }).FirstOrDefault();
        }

        public void Create(EmailConfirmation confirm)
        {
            confirm.token = Guid.NewGuid().ToString().ToUpper();

            Context.Connection.Query("INSERT INTO fso_email_confirm VALUES (@type, @email, @token, @expires, @verified)", confirm);
        }

        public void Remove(string token)
        {
            Context.Connection.Query("DELETE FROM fso_email_confirm WHERE token = @token", new { token = token });
        }
    }
}
