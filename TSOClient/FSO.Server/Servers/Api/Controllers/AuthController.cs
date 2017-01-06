using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.AuthTickets;
using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Api.Controllers
{
    public class AuthController : NancyModule
    {
        private const String ERROR_020_CODE = "INV-020";
        private const String ERROR_020_MSG = "Please enter your member name and password.";

        private const String ERROR_110_CODE = "INV-110";
        private const String ERROR_110_MSG = "The member name or password you have entered is incorrect. Please try again.";

        private const String ERROR_302_CODE = "INV-302";
        private const String ERROR_302_MSG = "The game has experienced an internal error. Please try again.";

        private const String ERROR_160_CODE = "INV-160";
        private const String ERROR_160_MSG = "The server is currently down for maintainance. Please try again later.";

        private IDAFactory DAFactory;
        private ApiServerConfiguration Config;

        public AuthController(IDAFactory daFactory, ApiServerConfiguration config)
        {
            this.DAFactory = daFactory;
            Config = config;
            this.Get["/AuthLogin"] = _ =>
            {
                var username = this.Request.Query["username"];
                var password = this.Request.Query["password"];
                var version = this.Request.Query["version"];

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    return Response.AsText(printError(ERROR_020_CODE, ERROR_020_MSG));
                }

                AuthTicket ticket = null;

                using (var db = DAFactory.Get())
                {
                    var user = db.Users.GetByUsername(username);
                    if (user == null || user.is_banned)
                    {
                        return Response.AsText(printError(ERROR_110_CODE, ERROR_110_MSG));
                    }
                    
                    if (config.Maintainance && !(user.is_admin || user.is_moderator))
                    {
                        return Response.AsText(printError(ERROR_160_CODE, ERROR_160_MSG));
                    }

                    var authSettings = db.Users.GetAuthenticationSettings(user.user_id);
                    var isPasswordCorrect = PasswordHasher.Verify(password, new PasswordHash
                    {
                        data = authSettings.data,
                        scheme = authSettings.scheme_class
                    });

                    if (!isPasswordCorrect)
                    {
                        return Response.AsText(printError(ERROR_110_CODE, ERROR_110_MSG));
                    }

                    var tryIP = Request.Headers["X-Forwarded-For"].FirstOrDefault();
                    if (tryIP != null) tryIP = tryIP.Substring(tryIP.LastIndexOf(',') + 1).Trim();
                    var ip = tryIP ?? this.Request.UserHostAddress;

                    var ban = db.Bans.GetByIP(ip);
                    if (ban != null)
                    {
                        return Response.AsText(printError(ERROR_110_CODE, ERROR_110_MSG));
                    }

                    /** Make a ticket **/
                    ticket = new AuthTicket();
                    ticket.ticket_id = Guid.NewGuid().ToString().Replace("-", "");
                    ticket.user_id = user.user_id;
                    ticket.date = Epoch.Now;
                    ticket.ip = ip;

                    db.AuthTickets.Create(ticket);
                }

                return Response.AsText("Valid=TRUE\r\nTicket=" + ticket.ticket_id.ToString() + "\r\n");
            };
        }
        

        public static string printError(String code, String message)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("Valid=FALSE");
            result.AppendLine("Ticket=0");
            result.AppendLine("reasontext=" + code + ";" + message);
            result.AppendLine("reasonurl=");

            return result.ToString();
        }
    }
}
