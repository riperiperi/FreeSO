using FSO.Server.Api.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA.EmailConfirmation;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Web.Http;

namespace FSO.Server.Api.Controllers
{
    /// <summary>
    /// Controller for user registrations.
    /// Supports email confirmation if enabled in config.json.
    /// </summary>
    public class RegistrationController : ApiController
    {
        private const int REGISTER_THROTTLE_SECS = 60;
        private const int EMAIL_CONFIRMATION_EXPIRE = 2 * 60 * 60; // 2 hrs

        /// <summary>
        /// Alphanumeric (lowercase), no whitespace or special chars, cannot start with an underscore.
        /// </summary>
        private static Regex USERNAME_VALIDATION = new Regex("^([a-z0-9]){1}([a-z0-9_]){2,23}$");


        [HttpPost]
        [Route("userapi/registration")]
        public HttpResponseMessage CreateUser(RegistrationModel user)
        {
            var api = Api.INSTANCE;

            // Check if we wanted to force email confirmation
            if(api.Config.EmailConfirmation)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "registration_failed",
                    error_description = "missing_confirmation_token"
                });
            }

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

            try
            {
                var addr = new System.Net.Mail.MailAddress(user.email);
            }
            catch
            {
                failReason = "email_invalid";
            }

            if (failReason != null)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "bad_request",
                    error_description = failReason
                });
            }

            if (!string.IsNullOrEmpty(api.Config.Regkey) && api.Config.Regkey != user.key)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "key_wrong",
                    error_description = failReason
                });
            }

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

                var userModel = api.CreateUser(user.username, user.email, user.password, ip);

                if(userModel==null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "registration_failed",
                        error_description = "user_exists"
                    });
                } else {
                    return ApiResponse.Json(HttpStatusCode.OK, userModel);
                }
            }
        }

        /// <summary>
        /// Create a confirmation token and send email.
        /// </summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("userapi/registration/token_create")]
        public HttpResponseMessage CreateToken(RegistrationCreateTokenModel model)
        {
            // To do: check if email address is disposable.
            Api api = Api.INSTANCE;

            if(model.confirmation_url==null||model.email==null)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "registration_failed",
                    error_description = "missing_fields"
                });
            }

            try
            {
                var addr = new System.Net.Mail.MailAddress(model.email);
            }
            catch
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "registration_failed",
                    error_description = "email_invalid"
                });
            }

            using (var da = api.DAFactory.Get())
            {
                if(da.Users.GetByEmail(model.email)!=null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "registration_failed",
                        error_description = "email_taken"
                    });
                }

                EmailConfirmation confirm = da.EmailConfirmations.GetByEmail(model.email, ConfirmationType.email);

                if(confirm!=null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "registration_failed",
                        error_description = "confirmation_pending"
                    });
                }

                uint expires = Epoch.Now + EMAIL_CONFIRMATION_EXPIRE;

                string token = da.EmailConfirmations.Create(new EmailConfirmation
                {
                    type = ConfirmationType.email,
                    email = model.email,
                    expires = expires
                });

                bool sent = api.SendEmailConfirmationMail(model.email, token, model.confirmation_url, expires);
                 
                if(sent)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new
                    {
                        status = "success"
                    });
                }

                return ApiResponse.Json(HttpStatusCode.OK, new
                {
                    status = "email_failed"
                });
               
            }
        }

        /// <summary>
        /// Create a user with a valid email confirmation token.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("userapi/registration/confirm")]
        public HttpResponseMessage CreateUserWithToken(RegistrationUseTokenModel user)
        {
            Api api = Api.INSTANCE;

            if (user == null)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "registration_failed",
                    error_description = "invalid_token"
                });
            }

            using (var da = api.DAFactory.Get())
            {
                EmailConfirmation confirmation = da.EmailConfirmations.GetByToken(user.token);

                if(confirmation == null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "registration_failed",
                        error_description = "invalid_token"
                    });
                }

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

                try
                {
                    var addr = new System.Net.Mail.MailAddress(user.email);
                }
                catch
                {
                    failReason = "email_invalid";
                }

                if (failReason != null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "bad_request",
                        error_description = failReason
                    });
                }

                if (!string.IsNullOrEmpty(api.Config.Regkey) && api.Config.Regkey != user.key)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "key_wrong",
                        error_description = failReason
                    });
                }

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
                var prev = da.Users.GetByRegisterIP(ip);
                if (Epoch.Now - (prev.FirstOrDefault()?.register_date ?? 0) < REGISTER_THROTTLE_SECS)
                {
                    //cannot create a new account this soon.
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "registration_failed",
                        error_description = "registrations_too_frequent"
                    });
                }

                var userModel = api.CreateUser(user.username, user.email, user.password, ip);

                if (userModel == null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "registration_failed",
                        error_description = "user_exists"
                    });
                }
                else
                {
                    api.SendEmailConfirmationOKMail(user.username, user.email);
                    return ApiResponse.Json(HttpStatusCode.OK, userModel);
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

    /// <summary>
    /// Expected request data when trying to create a token to register.
    /// </summary>
    public class RegistrationCreateTokenModel
    {
        public string email { get; set; }
        /// <summary>
        /// The link the user will have to go to in order to confirm their token.
        /// If %token% is present in the url, it will be replaced with the user's token.
        /// </summary>
        public string confirmation_url { get; set; }
    }

    /// <summary>
    /// Expected request data when trying to register with a token.
    /// </summary>
    public class RegistrationUseTokenModel
    {
        public string username { get; set; }
        /// <summary>
        /// User email.
        /// </summary>
        public string email { get; set; }
        /// <summary>
        /// User password.
        /// </summary>
        public string password { get; set; }
        /// <summary>
        /// Registration key.
        /// </summary>
        public string key { get; set; }
        /// <summary>
        /// The unique GUID.
        /// </summary>
        public string token { get; set; }
    }
}