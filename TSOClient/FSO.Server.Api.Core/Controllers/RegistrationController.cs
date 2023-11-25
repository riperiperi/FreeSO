using FSO.Server.Api.Core.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA.EmailConfirmation;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace FSO.Server.Api.Core.Controllers
{
    /// <summary>
    /// Controller for user registrations.
    /// Supports email confirmation if enabled in config.json.
    /// </summary>

    [EnableCors]
    [Route("userapi/registration")]
    [ApiController]
    public class RegistrationController : ControllerBase
    {
        private const int REGISTER_THROTTLE_SECS = 60;
        private const int EMAIL_CONFIRMATION_EXPIRE = 2 * 60 * 60; // 2 hrs

        /// <summary>
        /// Alphanumeric (lowercase), no whitespace or special chars, cannot start with an underscore.
        /// </summary>
        private static Regex USERNAME_VALIDATION = new Regex("^([a-z0-9]){1}([a-z0-9_]){2,23}$");

        #region Registration
        [HttpPost]
        public IActionResult CreateUser([FromForm] RegistrationModel user)
        {
            var api = Api.INSTANCE;

            if(api.Config.SmtpEnabled)
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
            //user.key = user.key ?? "";

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

            /*
            if (!string.IsNullOrEmpty(api.Config.Regkey) && api.Config.Regkey != user.key)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "key_wrong",
                    error_description = failReason
                });
            }
            */

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
                    api.SendEmailConfirmationOKMail(user.username, user.email);
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
        [Route("userapi/registration/request")]
        public IActionResult CreateToken(ConfirmationCreateTokenModel model)
        {
            Api api = Api.INSTANCE;

            // smtp needs to be configured for this
            if(!api.Config.SmtpEnabled)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "registration_failed",
                    error_description = "smtp_disabled"
                });
            }

            if(model.confirmation_url==null||model.email==null)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "registration_failed",
                    error_description = "missing_fields"
                });
            }

            // verify email syntax
            // To do: check if email address is disposable.
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
                // email is taken
                if(da.Users.GetByEmail(model.email)!=null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "registration_failed",
                        error_description = "email_taken"
                    });
                }

                EmailConfirmation confirm = da.EmailConfirmations.GetByEmail(model.email, ConfirmationType.email);

                // already waiting for confirmation
                if(confirm!=null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "registration_failed",
                        error_description = "confirmation_pending"
                    });
                }

                uint expires = Epoch.Now + EMAIL_CONFIRMATION_EXPIRE;

                // create new email confirmation
                string token = da.EmailConfirmations.Create(new EmailConfirmation
                {
                    type = ConfirmationType.email,
                    email = model.email,
                    expires = expires
                });

                // send email with recently generated token
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
        public IActionResult CreateUserWithToken(RegistrationUseTokenModel user)
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

                //create user in db
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
                    //send OK email
                    api.SendEmailConfirmationOKMail(user.username, user.email);
                    da.EmailConfirmations.Remove(user.token);
                    return ApiResponse.Json(HttpStatusCode.OK, userModel);
                }
            }
        }

        #endregion

        #region Password reset
        [HttpPost]
        [Route("userapi/password")]
        public IActionResult ChangePassword(PasswordResetModel model)
        {
            Api api = Api.INSTANCE;

            if (api.Config.SmtpEnabled)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "password_reset_failed",
                    error_description = "missing_confirmation_token"
                });
            }

            // No empty fields
            if (model.username==null||model.new_password==null||model.old_password==null)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "password_reset_failed",
                    error_description = "missing_fields"
                });
            }

            using (var da = api.DAFactory.Get())
            {
                var user = da.Users.GetByUsername(model.username);

                if(user==null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "password_reset_failed",
                        error_description = "user_invalid"
                    });
                }

                var authSettings = da.Users.GetAuthenticationSettings(user.user_id);
                var correct = PasswordHasher.Verify(model.old_password, new PasswordHash
                {
                    data = authSettings.data,
                    scheme = authSettings.scheme_class
                });

                if (!correct)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "password_reset_failed",
                        error_description = "incorrect_password"
                    });
                }

                api.ChangePassword(user.user_id, model.new_password);
                api.SendPasswordResetOKMail(user.email, user.username);

                return ApiResponse.Json(HttpStatusCode.OK, new
                {
                    status = "success"
                });
            }
        }

        /// <summary>
        /// Resets a user's password using a confirmation token.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("userapi/password/confirm")]
        public IActionResult ConfirmPwd(PasswordResetUseTokenModel model)
        {
            Api api = Api.INSTANCE;

            if(model.token==null||model.new_password==null)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "password_reset_failed",
                    error_description = "missing_fields"
                });
            }

            using (var da = api.DAFactory.Get())
            {
                EmailConfirmation confirmation = da.EmailConfirmations.GetByToken(model.token);

                if(confirmation==null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "password_reset_failed",
                        error_description = "invalid_token"
                    });
                }

                var user = da.Users.GetByEmail(confirmation.email);

                api.ChangePassword(user.user_id, model.new_password);
                api.SendPasswordResetOKMail(user.email, user.username);
                da.EmailConfirmations.Remove(model.token);

                return ApiResponse.Json(HttpStatusCode.OK, new
                {
                    status = "success"
                });
            }
        }

        /// <summary>
        /// Creates a password reset token and mails it to the user.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [Route("userapi/password/request")]
        public IActionResult CreatePwdToken(ConfirmationCreateTokenModel model)
        {  
            Api api = Api.INSTANCE;

            if (model.confirmation_url == null || model.email == null)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "password_reset_failed",
                    error_description = "missing_fields"
                });
            }

            // verify email syntax
            // To do: check if email address is disposable.
            try
            {
                var addr = new System.Net.Mail.MailAddress(model.email);
            }
            catch
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "password_reset_failed",
                    error_description = "email_invalid"
                });
            }

            using (var da = api.DAFactory.Get())
            {

                var user = da.Users.GetByEmail(model.email);

                if(user==null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "password_reset_failed",
                        error_description = "email_invalid"
                    });
                }

                EmailConfirmation confirm = da.EmailConfirmations.GetByEmail(model.email, ConfirmationType.password);

                // already awaiting a confirmation
                // to-do: resend?
                if (confirm != null)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                    {
                        error = "registration_failed",
                        error_description = "confirmation_pending"
                    });
                }

                uint expires = Epoch.Now + EMAIL_CONFIRMATION_EXPIRE;

                // create new email confirmation
                string token = da.EmailConfirmations.Create(new EmailConfirmation
                {
                    type = ConfirmationType.password,
                    email = model.email,
                    expires = expires
                });

                // send confirmation email with generated token
                bool sent = api.SendPasswordResetMail(model.email, user.username, token, model.confirmation_url, expires);

                if (sent)
                {
                    return ApiResponse.Json(HttpStatusCode.OK, new
                    {
                        status = "success"
                    });
                }

                return ApiResponse.Json(HttpStatusCode.OK, new
                {
                    // success but email shitfaced
                    status = "email_failed"
                });
            }
        }

        #endregion
    }

    #region Models
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
    }

    /// <summary>
    /// Expected request data when trying to create a token to register.
    /// </summary>
    public class ConfirmationCreateTokenModel
    {
        public string email { get; set; }
        /// <summary>
        /// The link the user will have to go to in order to confirm their token.
        /// If %token% is present in the url, it will be replaced with the user's token.
        /// </summary>
        public string confirmation_url { get; set; }
    }

    public class PasswordResetModel
    {
        public string username { get; set; }
        public string old_password { get; set; }
        public string new_password { get; set; }
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

    public class PasswordResetUseTokenModel
    {
        public string token { get; set; }
        public string new_password { get; set; }
    }

    #endregion
}