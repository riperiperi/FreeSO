using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.AuthTickets;
using FSO.Server.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Api.Controllers
{
    public class AuthController
    {
        private const String ERROR_020_CODE = "INV-020";
        private const String ERROR_020_MSG = "Please enter your EA member name and password. If you forgot your password, you can retrieve it at www.EA.com.";

        private const String ERROR_110_CODE = "INV-110";
        private const String ERROR_110_MSG = "The member name or password you have entered is incorrect. Please try again.";

        private const String ERROR_302_CODE = "INV-302";
        private const String ERROR_302_MSG = "The game has experienced an internal error. Please try again.";

        private IDAFactory DAFactory;

        public AuthController(ApiServerConfiguration config, HttpRouter router, IDAFactory daFactory)
        {
            this.DAFactory = daFactory;
            router.Get("/AuthLogin", new HttpHandler(OnAuthLogin));
        }

        private void OnAuthLogin(HttpListenerRequest request, HttpListenerResponse response)
        {
            var username = request.QueryString["username"];
            var password = request.QueryString["password"];
            var version = request.QueryString["version"];

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                printError(response, ERROR_020_CODE, ERROR_020_MSG);
                return;
            }

            AuthTicket ticket = null;

            using (var db = DAFactory.Get())
            {
                var user = db.Users.GetByUsername(username);
                if (user == null || user.is_banned)
                {
                    printError(response, ERROR_110_CODE, ERROR_110_MSG);
                    return;
                }

                var authSettings = db.Users.GetAuthenticationSettings(user.user_id);
                var isPasswordCorrect = PasswordHasher.Verify(password, new PasswordHash
                {
                    data = authSettings.data,
                    scheme = authSettings.scheme_class
                });

                if (!isPasswordCorrect)
                {
                    printError(response, ERROR_110_CODE, ERROR_110_MSG);
                    return;
                }

                /** Make a ticket **/
                ticket = new AuthTicket();
                ticket.ticket_id = Guid.NewGuid().ToString().Replace("-", "");
                ticket.user_id = user.user_id;
                ticket.date = Epoch.Now;
                ticket.ip = Common.IPAddress.Get(request);

                db.AuthTickets.Create(ticket);
            }

            response.StatusCode = 200;
            response.Send("Valid=TRUE\r\nTicket=" + ticket.ticket_id.ToString() + "\r\n");
        }

        private void printError(HttpListenerResponse response, String code, String message)
        {
            response.StatusCode = 200;
            StringBuilder result = new StringBuilder();
            result.AppendLine("Valid=FALSE");
            result.AppendLine("Ticket=0");
            result.AppendLine("reasontext=" + code + ";" + message);
            result.AppendLine("reasonurl=");

            response.ContentType = "text/plain";
            response.Send(result.ToString());
        }
    }
}
