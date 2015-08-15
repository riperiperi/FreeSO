using Nancy;
using Nancy.Authentication.Token;
using Nancy.Security;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Api.JsonWebToken
{
    public class JWTIdentityTokenizer : ITokenizer
    {
        public IUserIdentity Detokenize(string token, NancyContext context, IUserIdentityResolver userIdentityResolver)
        {
            return Newtonsoft.Json.JsonConvert.DeserializeObject<JWTUserIdentity>(token);
        }

        public string Tokenize(IUserIdentity userIdentity, NancyContext context)
        {
            return Newtonsoft.Json.JsonConvert.SerializeObject(userIdentity);
        }
    }
}
