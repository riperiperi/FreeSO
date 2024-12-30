using FSO.Server.Common.Config;
using System.Collections.Generic;

namespace FSO.Server.Servers.UserApi
{
    public class ApiServerConfiguration
    {
        /// <summary>
        /// If true, the API server will attempt to bind
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Hostname bindings
        /// </summary>
        public List<string> Bindings { get; set; }
        
        /// <summary>
        /// How long an auth ticket is valid for
        /// </summary>
        public int AuthTicketDuration = 300;

        /// <summary>
        /// If non-null, the user must provide this key to register an account.
        /// </summary>
        public string Regkey { get; set; }

        /// <summary>
        /// If true, only authentication from moderators and admins will be accepted
        /// </summary>
        public bool Maintenance { get; set; }
        public string UpdateUrl { get; set; }
        public string CDNUrl { get; set; }

        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpUser { get; set; }
        public bool UseProxy { get; set; } = true;

        public AWSConfig AwsConfig { get; set; }
        public GithubConfig GithubConfig { get; set; }
        public FilesystemConfig FilesystemConfig { get; set; }
    }

    public enum ApiServerControllers
    {
        Auth,
        CitySelector
    }
}
