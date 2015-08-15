using Nancy;
using Nancy.Authentication.Token;
using Nancy.Security;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Api.JsonWebToken
{
    public class JWTTokenAuthentication
    {
        private const string Scheme = "bearer";

        public static void Enable(INancyModule module, JWTConfiguration configuration)
        {
            if (module == null)
            {
                throw new ArgumentNullException("module");
            }

            module.Before.AddItemToStartOfPipeline(GetCredentialRetrievalHook(configuration));
        }

        private static Func<NancyContext, Response> GetCredentialRetrievalHook(JWTConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            return context =>
            {
                RetrieveCredentials(context, configuration);
                return null;
            };
        }

        private static void RetrieveCredentials(NancyContext context, JWTConfiguration configuration)
        {
            var token = ExtractTokenFromHeader(context.Request);
            if (token == null)
            {
                return;
            }

            try {
                var payload = JWT.JsonWebToken.Decode(token, configuration.Key, true);
                Dictionary<string, string> payloadParsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload);

                //identity
                var user = configuration.Tokenizer.Detokenize(payloadParsed["identity"], context, null);

                if (user != null) {
                    context.CurrentUser = user;
                }
            }catch(Exception ex){
                //Expired
            }
        }

        private static string ExtractTokenFromHeader(Request request)
        {
            var authorization = request.Headers.Authorization;

            if (string.IsNullOrEmpty(authorization))
            {
                return null;
            }

            if (!authorization.StartsWith(Scheme))
            {
                return null;
            }

            try
            {
                var encodedToken = authorization.Substring(Scheme.Length).Trim();
                return String.IsNullOrWhiteSpace(encodedToken) ? null : encodedToken;
            }
            catch (FormatException)
            {
                return null;
            }
        }
    }
}
