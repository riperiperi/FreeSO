using FSO.Server.Api.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA.Users;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;

namespace FSO.Server.Api.Controllers
{
    public class RegistrationController : ApiController
    {
        private const int REGISTER_THROTTLE_SECS = 60;

        /// <summary>
        /// Alphanumeric (lowercase), no whitespace or special chars, cannot start with an underscore.
        /// </summary>
        private static Regex USERNAME_VALIDATION = new Regex("^([a-z0-9]){1}([a-z0-9_]){2,23}$");

        [HttpPost]
        public HttpResponseMessage Post(HttpRequestMessage request, [FromBody] RegistrationModel user)
        {
            var api = Api.INSTANCE;
            var ip = ApiUtils.GetIP(Request);

            user.username = user.username ?? "";
            user.username = user.username.ToLowerInvariant();
            user.email = user.email ?? "";
            user.key = user.key ?? "";

            string failReason = null;
            if (user.username.Length < 3) failReason = "user_short";
            else if (user.username.Length > 24) failReason = "user_long";
            else if (!USERNAME_VALIDATION.IsMatch(user.username ?? "")) failReason = "user_invalid";
            else if ((user.password?.Length ?? 0) == 0) failReason = "pass_required";

            if (failReason != null)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "bad_request",
                    error_description = failReason
                });
            }

            bool isAdmin = false;
            if (!string.IsNullOrEmpty(api.Config.Regkey) && api.Config.Regkey != user.key)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "key_wrong",
                    error_description = failReason
                });
            }

            var passhash = PasswordHasher.Hash(user.password);

            using (var da = api.DAFactory.Get())
            {
                //has this ip been banned?
                var ban = da.Bans.GetByIP(ip);
                if (ban != null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "registration_failed",
                        error_description = "ip_banned"
                    });
                }

                //has this user registered a new account too soon after their last?
                var now = Epoch.Now;
                var prev = da.Users.GetByRegisterIP(ip);
                if (now - (prev.FirstOrDefault()?.register_date ?? 0) < REGISTER_THROTTLE_SECS)
                {
                    //cannot create a new account this soon.
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "registration_failed",
                        error_description = "registrations_too_frequent"
                    });
                }

                //TODO: is this ip banned?

                var userModel = new User();
                userModel.username = user.username;
                userModel.email = user.email;
                userModel.is_admin = isAdmin;
                userModel.is_moderator = isAdmin;
                userModel.user_state = UserState.valid;
                userModel.register_date = now;
                userModel.is_banned = false;
                userModel.register_ip = ip;
                userModel.last_ip = ip;

                var authSettings = new UserAuthenticate();
                authSettings.scheme_class = passhash.scheme;
                authSettings.data = passhash.data;

                try
                {
                    var userId = da.Users.Create(userModel);
                    authSettings.user_id = userId;
                    da.Users.CreateAuth(authSettings);

                    userModel = da.Users.GetById(userId);
                    if (userModel == null) { throw new Exception("Unable to find user"); }
                    return ApiResponse.Json(HttpStatusCode.OK, userModel);
                }
                catch (Exception)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "registration_failed",
                        error_description = "user_exists"
                    });
                }

            }
        }
    }

    public class RegistrationError
    {
        public string error_description { get; set; }
        public string error { get; set; }
    }

    public class RegistrationModel
    {
        public string username { get; set; }
        public string email { get; set; }
        public string password { get; set; }
        public string key { get; set; }
    }
}