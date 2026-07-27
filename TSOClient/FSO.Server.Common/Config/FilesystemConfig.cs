using Newtonsoft.Json;

namespace FSO.Server.Common.Config
{
    public class FilesystemConfig
    {
        [JsonProperty("basePath")]
        public string BasePath { get; set; } = "./public";
        [JsonProperty("baseURL")]
        public string BaseURL { get; set; }
    }
}
