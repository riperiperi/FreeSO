using System;
using System.Collections.Generic;
using System.Text;

namespace TSO_CityServer.Network
{
    /// <summary>
    /// A client's token + the character's GUID, as received by the LoginServer.
    /// </summary>
    public class ClientToken
    {
        public string ClientIP = "";
        public string CharacterGUID = "";
        public string Token = "";
    }
}
