using FSO.Server.Api.Core.Utils;
using FSO.Server.Common;
using FSO.Server.Servers.Api.JsonWebToken;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;

namespace FSO.Server.Api.Core.Controllers
{
    [Route("userapi/oauth/token")]
    [ApiController]
    public class UserOAuthController : ControllerBase
    {
        [HttpPost]
        public IActionResult CreateToken([FromForm] UserOAuthRequest userAuthRequest)
        {
            if (userAuthRequest == null) BadRequest();
            var api = Api.INSTANCE;
            using (var da = api.DAFactory.Get())
            {
                var user = da.Users.GetByUsername(userAuthRequest.username);
                if (user == null || user.is_banned) return ApiResponse.Json(System.Net.HttpStatusCode.Unauthorized, new UserOAuthError("unauthorized_client", "user_credentials_invalid"));
                var ip = ApiUtils.GetIP(Request);
                var hashSettings = da.Users.GetAuthenticationSettings(user.user_id);
                var isPasswordCorrect = PasswordHasher.Verify(userAuthRequest.password, new PasswordHash
                {
                    data = hashSettings.data,
                    scheme = hashSettings.scheme_class
                });
                //check if account is locked due to failed attempts
                var accLock = da.Users.GetRemainingAuth(user.user_id, ip);
                if (accLock != null && (accLock.active || accLock.count >= AuthLoginController.LockAttempts) && accLock.expire_time > Epoch.Now)
                {
                    return ApiResponse.Json(System.Net.HttpStatusCode.OK, new UserOAuthError("unauthorized_client", "account_locked"));
                }
                //if the password is incorrect and check if user failed muli times and set a time out till next try.
                if (!isPasswordCorrect)
                {
                    var durations = AuthLoginController.LockDuration;
                    var failDelay = 60 * durations[Math.Min(durations.Length - 1, da.Users.FailedConsecutive(user.user_id, ip))];
                    if (accLock == null)
                    {
                        da.Users.NewFailedAuth(user.user_id, ip, (uint)failDelay);
                    }
                    else
                    {
                        var remaining = da.Users.FailedAuth(accLock.attempt_id, (uint)failDelay, AuthLoginController.LockAttempts);
                    }
                    return ApiResponse.Json(System.Net.HttpStatusCode.OK, new UserOAuthError("unauthorized_client", "user_credentials_invalid"));
                }

                //user passed the password check, and now creates the claim/token
                da.Users.SuccessfulAuth(user.user_id, ip);
                var claims = new List<string>();

                //set the permission level in the claim
                switch (userAuthRequest.permission_level)
                {
                    case 1:
                        claims.Add("userReadPermissions");
                        break;
                    case 2:
                        claims.Add("userReadPermissions");
                        claims.Add("userWritePermissions");
                        break;
                    case 3:
                        claims.Add("userReadPermissions");
                        claims.Add("userWritePermissions");
                        claims.Add("userUpdatePermissions");
                        break;
                    case 4:
                        claims.Add("userReadPermissions");
                        claims.Add("userWritePermissions");
                        claims.Add("userUpdatePermissions");
                        claims.Add("userDeletePermissions");
                        break;
                    default:
                        break;
                }
                
                //set the user identity
                JWTUser identity = new JWTUser
                {
                    UserID = user.user_id,
                    UserName = user.username,
                    Claims = claims
                };

                //generate the the tokenen and send it in a JSON format as response
                var generatedToken = api.JWT.CreateToken(identity);
                return ApiResponse.Json(System.Net.HttpStatusCode.OK, new UserOAuthSuccess
                {
                    access_token = generatedToken.Token,
                    expires_in = generatedToken.ExpiresIn
                });
            }
            
        }
    }
    public class UserOAuthRequest
    {
        public int permission_level { get; set; }
        public string username { get; set; }
        public string password { get; set; }
    }
    public class UserOAuthError
    {
        public string error;
        public string error_description;
        public UserOAuthError(string errorString,string errorDescriptionString)
        {
            error = errorString;
            error_description = errorDescriptionString;
        }
    }
    public class UserOAuthSuccess
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
    }

}