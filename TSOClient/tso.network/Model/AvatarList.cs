using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace TSOServiceClient.Model
{
    public class AvatarList
    {
        [JsonProperty("avatars")]
        public List<AvatarInfo> Avatars;
    }

    public class AvatarInfo
    {
        [JsonProperty("name")]
        public string Name;

        [JsonProperty("uuid")]
        public string UUID;

        [JsonProperty("cityId")]
        public int CityId;

        [JsonProperty("description")]
        public string Description;
    }

}
