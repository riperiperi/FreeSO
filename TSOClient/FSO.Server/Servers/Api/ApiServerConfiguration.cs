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
        /// Indicates which routes to register on the api
        /// </summary>
        public List<ApiServerRoutes> Routes { get; set; }
    }

    public enum ApiServerRoutes
    {
        Auth,
        CitySelector
    }
}
