using System;
using System.Collections.Generic;
using System.Text;

namespace TSO_CityServer.Network
{
    /// <summary>
    /// A client's token, as received by the LoginServer.
    /// </summary>
    public class ClientToken
    {
        public string ClientIP = "";
        public string Token = "";
    }
}
