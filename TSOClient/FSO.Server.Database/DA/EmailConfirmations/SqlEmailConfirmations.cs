using System;
using System.Linq;
using Dapper;
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
            Context.Connection.Execute("INSERT INTO fso_email_confirm (type, email, token, expires) VALUES (@type, @email, @token, @expires)", confirm);
            return confirm.token;
        }

        public void Remove(string token)
        {
            Context.Connection.Execute("DELETE FROM fso_email_confirm WHERE token = @token", new { token = token });
        }
    }
}
