using Nancy.Authentication.Token;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Api.JsonWebToken
{
    public class JWTConfiguration
    {
        public byte[] Key;
        public ITokenizer Tokenizer = new JWTIdentityTokenizer();
        public int TokenDuration = 3600;
    }
}
