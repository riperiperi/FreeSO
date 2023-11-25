using FSO.Server.Api.Core.Services;

namespace FSO.Server.Api.Core
{
    public class ApiConfig
    {
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
        public bool Maintainance { get; set; }

        public string Secret { get; set; }

        public string UpdateUrl { get; set; }
        public string CDNUrl { get; set; }

        public string NFSdir { get; set; }

        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; }
        public string SmtpPassword { get; set; }
        public string SmtpUser { get; set; }

        public bool SmtpEnabled { get; set;  }
        public bool UseProxy { get; set; }

        public int? UpdateID { get; set; }

        public string BranchName { get; set; } = "dev";
        public IUpdateUploader UpdateUploader { get; set; }
    }
}