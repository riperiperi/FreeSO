using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace TSOServiceClient.Model
{
    public class AuthRequest
    {
        [JsonProperty("user")]
        public string Username;

        [JsonProperty("pass")]
        public string Password;
    }
}
