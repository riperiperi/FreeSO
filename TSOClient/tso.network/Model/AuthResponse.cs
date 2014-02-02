using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace TSOServiceClient.Model
{
    public class AuthResponse
    {
        [JsonProperty("valid")]
        public bool Valid;

        [JsonProperty("uid")]
        public string UID;

        [JsonProperty("sessionID")]
        public string SessionID;

        [JsonProperty("sessionStart")]
        public DateTime SessionStart;

        [JsonProperty("sessionEnd")]
        public DateTime SessionEnd;
    }
}
