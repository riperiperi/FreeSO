using Newtonsoft.Json;

namespace FSO.Server.Framework.Aries
{
    public abstract class AbstractAriesServerConfig
    {
        [JsonProperty("call_sign")]
        public string Call_Sign;
        [JsonProperty("certificate")]
        public string Certificate;
        [JsonProperty("binding")]
        public string Binding;
        [JsonProperty("internal_host")]
        public string Internal_Host;
        [JsonProperty("public_host")]
        public string Public_Host;
        [JsonProperty("use_ssl")]
        public bool? Use_SSL;
    }
}
