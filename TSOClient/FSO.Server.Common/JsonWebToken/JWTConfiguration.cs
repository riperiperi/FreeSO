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
        public int TokenDuration = 3600;
    }
}
