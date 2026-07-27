using Newtonsoft.Json;

namespace FSO.Server.Common.Config
{
    public class AWSConfig
    {
        [JsonProperty("region")]
        public string Region { get; set; } = "eu-west-2";
        [JsonProperty("bucket")]
        public string Bucket { get; set; } = "fso-updates";
        [JsonProperty("accessKeyID")]
        public string AccessKeyID { get; set; }
        [JsonProperty("secretAccessKey")]
        public string SecretAccessKey { get; set; }
    }
}
