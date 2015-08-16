using FSO.Server.Common;
using FSO.Server.Database.DA;
using FSO.Server.Servers.Api.JsonWebToken;
using Nancy;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Api.Controllers.Admin
{
    public class AdminOAuthController : NancyModule
    {
        public AdminOAuthController(IDAFactory daFactory, JWTConfiguration oauthTokenConfig) : base("/admin/oauth")
        {
            this.Post["/token"] = _ =>
            {
                var grant_type = this.Request.Form["grant_type"];

                if (grant_type == "password")
                {
                    var username = this.Request.Form["username"];
                    var password = this.Request.Form["password"];

                    using (var da = daFactory.Get())
                    {
                        var user = da.Users.GetByUsername(username);
                        if (user == null || user.is_banned || (!user.is_admin || !user.is_moderator))
                        {
                            return Response.AsJson<OAuthError>(new OAuthError
                            {
                                error = "unauthorized_client",
                                error_description = "user_credentials_invalid"
                            });
                        }

                        var authSettings = da.Users.GetAuthenticationSettings(user.user_id);
                        var isPasswordCorrect = PasswordHasher.Verify(password, new PasswordHash
                        {
                            data = authSettings.data,
                            scheme = authSettings.scheme_class
                        });

                        if (!isPasswordCorrect)
                        {
                            return Response.AsJson<OAuthError>(new OAuthError
                            {
                                error = "unauthorized_client",
                                error_description = "user_credentials_invalid"
                            });
                        }

                        JWTUserIdentity identity = new JWTUserIdentity();
                        identity.UserName = user.username;
                        var claims = new List<string>();
                        if (user.is_admin || !user.is_moderator)
                        {
                            claims.Add("moderator");
                        }
                        if (user.is_admin)
                        {
                            claims.Add("admin");
                        }

                        identity.Claims = claims;
                        identity.UserID = user.user_id;

                        var unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
                        var expires = Math.Round((DateTime.UtcNow - unixEpoch).TotalSeconds) + oauthTokenConfig.TokenDuration;
                        var payload = new Dictionary<string, object>()
                        {
                            { "exp", expires },
                            { "identity", oauthTokenConfig.Tokenizer.Tokenize(identity, this.Context) }
                        };

                        var token =
                            JWT.JsonWebToken.Encode(payload, oauthTokenConfig.Key, JWT.JwtHashAlgorithm.HS384);

                        return Response.AsJson<OAuthSuccess>(new OAuthSuccess
                        {
                            access_token = token,
                            expires_in = oauthTokenConfig.TokenDuration
                        });
                    }
                }

                return Response.AsJson<OAuthError>(new OAuthError
                {
                    error = "invalid_request",
                    error_description = "unknown grant_type"
                });
            };
        }
    }


    public class OAuthError
    {
        public string error_description { get; set; }
        public string error { get; set; }
    }

    public class OAuthSuccess
    {
        public string access_token { get; set; }
        public int expires_in { get; set; }
    }
}
