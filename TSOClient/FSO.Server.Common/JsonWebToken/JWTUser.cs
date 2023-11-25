using System.Collections.Generic;

namespace FSO.Server.Servers.Api.JsonWebToken
{
    public class JWTUser
    {
        public uint UserID { get; set; }

        public IEnumerable<string> Claims
        {
            get; set;
        }

        public string UserName
        {
            get; set;
        }
    }
}
