using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dapper;
using FSO.Server.Database.DA.Utils;
using FSO.Server.Common;

namespace FSO.Server.Database.DA.EmailConfirmation
{
    public class SqlEmailConfirmations : AbstractSqlDA, IEmailConfirmations
    {
        public SqlEmailConfirmations(ISqlContext context) : base(context)
        {

        }

        public EmailConfirmation GetByToken(string token)
        {
            var confirm = Context.Connection.Query<EmailConfirmation>("SELECT * FROM fso_email_confirm WHERE token = @token", new { token = token }).FirstOrDefault();
            
            if(confirm==null) { return null; }

            if(Epoch.Now > confirm.expires)
            {
                Remove(confirm.token);
                return null;
            }

            return confirm;
        }

        public EmailConfirmation GetByEmail(string email, ConfirmationType type)
        {
            var confirm = Context.Connection.Query<EmailConfirmation>("SELECT * FROM fso_email_confirm WHERE email = @email AND type = @type", new { email = email, type = type }).FirstOrDefault();

            if (confirm == null) { return null; }

            if (Epoch.Now > confirm.expires)
            {
                Remove(confirm.token);
                return null;
            }

            return confirm;
        }

        public string Create(EmailConfirmation confirm)
        {
            confirm.token = Guid.NewGuid().ToString().ToUpper();
            Context.Connection.Query("INSERT INTO fso_email_confirm (type, email, token, expires) VALUES (@type, @email, @token, @expires)", confirm);
            return confirm.token;
        }

        public void Remove(string token)
        {
            Context.Connection.Query("DELETE FROM fso_email_confirm WHERE token = @token", new { token = token });
        }
    }
}
