using FSO.Server.Api.Core.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA.AuthTickets;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Net;
using System.Text;

namespace FSO.Server.Api.Core.Controllers
{
    [Route("AuthLogin")]
    [ApiController]
    public class AuthLoginController : ControllerBase
    {
        private static Func<IActionResult> ERROR_020 = printError("INV-020", "Please enter your member name and password.");
        private static Func<IActionResult> ERROR_110 = printError("INV-110", "The member name or password you have entered is incorrect. Please try again.");
        private static Func<IActionResult> ERROR_302 = printError("INV-302", "The game has experienced an internal error. Please try again.");
        private static Func<IActionResult> ERROR_160 = printError("INV-160", "The server is currently down for maintainance. Please try again later.");
        private static Func<IActionResult> ERROR_150 = printError("INV-150", "We're sorry, but your account has been suspended or cancelled.");
        private static string LOCK_MESSAGE = "Your account has been locked due to too many incorrect login attempts. " +
            "If you cannot remember your password, it can be reset at https://beta.freeso.org/forgot. Locked for: ";

        public static int LockAttempts = 5;

        public static int[] LockDuration = new int[] {
            5,
            15,
            30,
            60,
            120,
            720,
            1440
        };

        // GET api/<controller>
        [HttpGet]
        public IActionResult Get(string username, string password, string version, string clientid)
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
                if (user == null)
                {
                    return ERROR_110();
                }

                if (user.is_banned)
                {
                    return ERROR_150();
                }

                if (api.Config.Maintainance && !(user.is_admin || user.is_moderator))
                {
                    return ERROR_160();
                }

                var ip = ApiUtils.GetIP(Request);

                var accLock = db.Users.GetRemainingAuth(user.user_id, ip);
                if (accLock != null && (accLock.active || accLock.count >= LockAttempts) && accLock.expire_time > Epoch.Now)
                {
                    return printError("INV-170", LOCK_MESSAGE + Epoch.HMSRemaining(accLock.expire_time))();
                }

                var authSettings = db.Users.GetAuthenticationSettings(user.user_id);
                var isPasswordCorrect = PasswordHasher.Verify(password, new PasswordHash
                {
                    data = authSettings.data,
                    scheme = authSettings.scheme_class
                });

                if (!isPasswordCorrect)
                {
                    var failDelay = 60 * LockDuration[Math.Min(LockDuration.Length - 1, db.Users.FailedConsecutive(user.user_id, ip))];
                    if (accLock == null)
                    {
                        db.Users.NewFailedAuth(user.user_id, ip, (uint)failDelay);
                    } else
                    {
                        var remaining = db.Users.FailedAuth(accLock.attempt_id, (uint)failDelay, LockAttempts);
                        if (remaining == 0)
                            return printError("INV-170", LOCK_MESSAGE + Epoch.HMSRemaining(Epoch.Now + (uint)failDelay))();
                    }
                    return ERROR_110();
                }
                
                var ban = db.Bans.GetByIP(ip);
                if (ban != null)
                {
                    return ERROR_110();
                }

                db.Users.SuccessfulAuth(user.user_id, ip);
                db.Users.UpdateClientID(user.user_id, clientid ?? "0");

                /** Make a ticket **/
                ticket = new AuthTicket();
                ticket.ticket_id = Guid.NewGuid().ToString().Replace("-", "");
                ticket.user_id = user.user_id;
                ticket.date = Epoch.Now;
                ticket.ip = ip;

                db.AuthTickets.Create(ticket);
                db.Users.UpdateLastLogin(user.user_id, Epoch.Now);
            }
            var content = "Valid=TRUE\r\nTicket=" + ticket.ticket_id.ToString() + "\r\n";
            return ApiResponse.Plain(HttpStatusCode.OK, content);
        }


        public static Func<IActionResult> printError(String code, String message)
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