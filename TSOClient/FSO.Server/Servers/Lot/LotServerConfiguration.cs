using FSO.Common;
using FSO.Server.Framework.Aries;
using Newtonsoft.Json;

namespace FSO.Server.Servers.Lot
{
    public class LotServerConfiguration : AbstractAriesServerConfig
    {
        [JsonProperty("max_lots")]
        public int Max_Lots = 1;

        /// <summary>
        /// Number of simulation ticks per tick sent to the client.
        /// Higher values reduce network load but increase latency.
        /// </summary>
        [JsonProperty("tick_rate_divider")]
        public int Tick_Rate_Divider = 4;

        /// <summary>
        /// Frequency to sync hollow lots when build/buy changes are made in milliseconds.
        /// By default, hollow lots are only synced when build/buy mode is exited. (0)
        /// </summary>
        [JsonProperty("hollow_sync_frequency")]
        public int Hollow_Sync_Frequency = 0;

        [JsonProperty("simNFS")]
        public string SimNFS;
        [JsonProperty("ringBufferSize")]
        public int RingBufferSize = 10;
        [JsonProperty("timeout_no_auth")]
        public bool Timeout_No_Auth = true;
        [JsonProperty("logJobLots")]
        public bool LogJobLots = false;

        //Which cities to provide lot hosting for
        [JsonProperty("cities")]
        public LotServerConfigurationCity[] Cities;

        //How often to reconnect lost connections to city servers and report capacity
        [JsonProperty("cityReportingInterval")]
        public int CityReportingInterval = 10000;

        // Copied from base config
        public bool AllOpenable;
        public ArchiveConfiguration Archive;
    }

    public class LotServerConfigurationCity
    {
        [JsonProperty("id")]
        public int ID;
        [JsonProperty("host")]
        public string Host;
    }
}
