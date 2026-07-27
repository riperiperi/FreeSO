using FSO.Server.Common.Config;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace FSO.Server.Servers.UserApi
{
    public class ApiServerConfiguration
    {
        /// <summary>
        /// If true, the API server will attempt to bind
        /// </summary>
        [JsonProperty("enabled")]
        public bool Enabled { get; set; }

        /// <summary>
        /// Hostname bindings
        /// </summary>
        [JsonProperty("bindings")]
        public List<string> Bindings { get; set; }

        /// <summary>
        /// How long an auth ticket is valid for
        /// </summary>
        [JsonProperty("authTicketDuration")]
        public int AuthTicketDuration = 300;

        /// <summary>
        /// If non-null, the user must provide this key to register an account.
        /// </summary>
        [JsonProperty("regkey")]
        public string Regkey { get; set; }

        /// <summary>
        /// If true, only authentication from moderators and admins will be accepted
        /// </summary>
        [JsonProperty("maintenance")]
        public bool Maintenance { get; set; }
        [JsonProperty("updateUrl")]
        public string UpdateUrl { get; set; }
        [JsonProperty("cdnUrl")]
        public string CDNUrl { get; set; }

        [JsonProperty("smtpHost")]
        public string SmtpHost { get; set; }
        [JsonProperty("smtpPort")]
        public int SmtpPort { get; set; }
        [JsonProperty("smtpPassword")]
        public string SmtpPassword { get; set; }
        [JsonProperty("smtpUser")]
        public string SmtpUser { get; set; }
        [JsonProperty("useProxy")]
        public bool UseProxy { get; set; } = true;

        [JsonProperty("awsConfig")]
        public AWSConfig AwsConfig { get; set; }
        [JsonProperty("githubConfig")]
        public GithubConfig GithubConfig { get; set; }
        [JsonProperty("filesystemConfig")]
        public FilesystemConfig FilesystemConfig { get; set; }
    }

    public enum ApiServerControllers
    {
        Auth,
        CitySelector
    }
}
