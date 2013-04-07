using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace TSOServiceClient.Model
{
    public class CityList
    {
        [JsonProperty("cities")]
        public List<CityInfo> Cities;
    }

    public class CityInfo
    {
        [JsonProperty("id")]
        public int ID;

        [JsonProperty("name")]
        public string Name;

        [JsonProperty("uuid")]
        public string UUID;

        [JsonProperty("map")]
        public string Map;

        [JsonProperty("online")]
        public bool Online;

        [JsonProperty("status")]
        public CityInfoStatus Status;

        [JsonProperty("motd")]
        public List<CityInfoMessageOfTheDay> Messages;
    }

    public class CityInfoMessageOfTheDay {
        [JsonProperty("from")]
        public string From;
        [JsonProperty("subject")]
        public string Subject;
        [JsonProperty("body")]
        public string Body;
    }

    public enum CityInfoStatus
    {
        Ok = 1,
        Busy = 2,
        Full = 3,
        Reserved = 4
    }
}
