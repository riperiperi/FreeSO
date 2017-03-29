using FSO.Server.Api.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Database.DA.AuthTickets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web.Http;

namespace FSO.Server.Api.Controllers
{
    public class AuthLoginController : ApiController
    {
        private static Func<HttpResponseMessage> ERROR_020 = printError("INV-020", "Please enter your member name and password.");
        private static Func<HttpResponseMessage> ERROR_110 = printError("INV-110", "The member name or password you have entered is incorrect. Please try again.");
        private static Func<HttpResponseMessage> ERROR_302 = printError("INV-302", "The game has experienced an internal error. Please try again.");
        private static Func<HttpResponseMessage> ERROR_160 = printError("INV-160", "The server is currently down for maintainance. Please try again later.");

        /// <summary>
        /// If no parameters provided, return error
        /// </summary>
        /// <returns></returns>
        public HttpResponseMessage Get()
        {
            return ERROR_020();
        }

        public HttpResponseMessage Get(string username, string password, string version)
        {
            return Get(username, password, version, null);
        }

        // GET api/<controller>
        public HttpResponseMessage Get(string username, string password, string version, string clientid)
        {
            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                return ERROR_020();
            }

            AuthTicket ticket = null;

            var api = Api.INSTANCE;

            using (var db = api.DAFactory.Get())
            {
                var user = db.Users.GetByUsername(username);
                if (user == null || user.is_banned)
                {
                    return ERROR_110();
                }

                if (api.Config.Maintainance && !(user.is_admin || user.is_moderator))
                {
                    return ERROR_160();
                }

                var authSettings = db.Users.GetAuthenticationSettings(user.user_id);
                var isPasswordCorrect = PasswordHasher.Verify(password, new PasswordHash
                {
                    data = authSettings.data,
                    scheme = authSettings.scheme_class
                });

                if (!isPasswordCorrect)
                {
                    return ERROR_110();
                }

                var ip = ApiUtils.GetIP(Request);

                var ban = db.Bans.GetByIP(ip);
                if (ban != null)
                {
                    return ERROR_110();
                }

                db.Users.UpdateClientID(user.user_id, clientid ?? "0");

                /** Make a ticket **/
                ticket = new AuthTicket();
                ticket.ticket_id = Guid.NewGuid().ToString().Replace("-", "");
                ticket.user_id = user.user_id;
                ticket.date = Epoch.Now;
                ticket.ip = ip;

                db.AuthTickets.Create(ticket);
            }

            var content = "Valid=TRUE\r\nTicket=" + ticket.ticket_id.ToString() + "\r\n";
            return ApiResponse.Plain(HttpStatusCode.OK, content);
        }
        

        public static Func<HttpResponseMessage> printError(String code, String message)
        {
            StringBuilder result = new StringBuilder();
            result.AppendLine("Valid=FALSE");
            result.AppendLine("Ticket=0");
            result.AppendLine("reasontext=" + code + ";" + message);
            result.AppendLine("reasonurl=");

            return () =>
            {
                return ApiResponse.Plain(HttpStatusCode.OK, result.ToString());
            };
        }
    }
}