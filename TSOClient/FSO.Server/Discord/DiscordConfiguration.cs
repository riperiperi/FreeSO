using Newtonsoft.Json;

namespace FSO.Server.Discord
{
    public class DiscordConfiguration
    {
        [JsonProperty("apiKey")]
        public string ApiKey;
        [JsonProperty("serverID")]
        public ulong ServerID;

        [JsonProperty("eventModChannelID")]
        public ulong EventModChannelID;
        [JsonProperty("eventPublicChannelID")]
        public ulong EventPublicChannelID;
        [JsonProperty("statusChannelID")]
        public ulong StatusChannelID;
    }
}
