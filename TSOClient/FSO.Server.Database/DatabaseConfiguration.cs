using Newtonsoft.Json;

namespace FSO.Server.Database
{
    public class DatabaseConfiguration
    {
        [JsonProperty("engine")]
        public string Engine { get; set; } = "mysql";
        [JsonProperty("connectionString")]
        public string ConnectionString { get; set; }
    }
}
