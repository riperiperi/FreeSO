﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Server.Servers.Api
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
        /// Indicates which routes to register on the api
        /// </summary>
        public List<ApiServerControllers> Controllers { get; set; }
        
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

        public string UpdateUrl { get; set; }

        public string MailerHost { get; set; }
        public int MailerPort { get; set; }

        public string MailerPassword { get; set; }

        public string MailerUser { get; set; }
    }

    public enum ApiServerControllers
    {
        Auth,
        CitySelector
    }
}
