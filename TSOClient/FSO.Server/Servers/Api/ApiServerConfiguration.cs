using System;
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
        /// The base URL used when serving client updates. [url]/client-###.zip
        /// </summary>
        public string UpdateBaseURL = null;
    }

    public enum ApiServerControllers
    {
        Auth,
        CitySelector
    }
}
