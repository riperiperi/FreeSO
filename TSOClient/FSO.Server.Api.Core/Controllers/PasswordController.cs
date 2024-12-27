using FSO.Server.Api.Core.Utils;
using FSO.Server.Common;
using FSO.Server.Database.DA.EmailConfirmation;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace FSO.Server.Api.Core.Controllers
{
    /// <summary>
    /// Controller for user password changes.
    /// Supports email confirmation if enabled in config.json.
    /// </summary>

    [EnableCors]
    [Route("userapi/password")]
    [ApiController]
    public class PasswordController
    {
        private const int EMAIL_CONFIRMATION_EXPIRE = 2 * 60 * 60; // 2 hrs

        #region Password reset
        [HttpPost]
        public IActionResult ChangePassword([FromForm] PasswordResetModel model)
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
            if (model.username == null || model.new_password == null || model.old_password == null)
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

                if (user == null)
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
        [Route("confirm")]
        public IActionResult ConfirmPwd([FromForm] PasswordResetUseTokenModel model)
        {
            Api api = Api.INSTANCE;

            if (model.token == null || model.new_password == null)
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

                if (confirmation == null)
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
        [Route("request")]
        public IActionResult CreatePwdToken([FromForm] ConfirmationCreateTokenModel model)
        {
            Api api = Api.INSTANCE;

            // smtp needs to be configured for this
            if (!api.Config.SmtpEnabled)
            {
                return ApiResponse.Json(HttpStatusCode.OK, new RegistrationError()
                {
                    error = "registration_failed",
                    error_description = "smtp_disabled"
                });
            }

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

                if (user == null)
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
                    // success, but failed to send the token email...
                    status = "email_failed"
                });
            }
        }

        #endregion
    }

    #region Models
    public class PasswordResetModel
    {
        public string username { get; set; }
        public string old_password { get; set; }
        public string new_password { get; set; }
    }

    public class PasswordResetUseTokenModel
    {
        public string token { get; set; }
        public string new_password { get; set; }
    }
    #endregion
}
