using FSO.Server.Common;
using JWT.Algorithms;
using JWT.Builder;
using Newtonsoft.Json;

namespace FSO.Server.Servers.Api.JsonWebToken
{
    public class JWTInstance
    {
        public string Token;
        public int ExpiresIn;
    }

    public class JWTFactory
    {
        private JWTConfiguration Config;

        public JWTFactory(JWTConfiguration config)
        {
            this.Config = config;
        }

        public JWTUser DecodeToken(string token)
        {
            var payload = JwtBuilder.Create().WithAlgorithm(new HMACSHA384Algorithm()).WithSecret(Config.Key).Decode(token);
            Dictionary<string, string> payloadParsed = JsonConvert.DeserializeObject<Dictionary<string, string>>(payload);
            return Newtonsoft.Json.JsonConvert.DeserializeObject<JWTUser>(payloadParsed["data"]);
        }

        public JWTInstance CreateToken(JWTUser data)
        {
            var tokenData = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            return CreateToken(tokenData, Config.TokenDuration);
        }

        private JWTInstance CreateToken(string data, int expiresIn)
        {
            var expires = Epoch.Now + expiresIn;
            var payload = new Dictionary<string, object>()
            {
                { "exp", expires },
                { "data", data }
            };

            var token = JwtBuilder.Create().WithAlgorithm(new HMACSHA384Algorithm()).WithSecret(Config.Key).Encode(payload);
            return new JWTInstance { Token = token, ExpiresIn = expiresIn };
        }
    }
}
