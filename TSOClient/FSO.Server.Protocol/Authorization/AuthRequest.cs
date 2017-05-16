using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Server.Protocol.Authorization
{
    public class AuthRequest
    {
        public string Username;
        public string Password;
        public string ServiceID;
        public string Version;
        public string ClientID;
    }
}
