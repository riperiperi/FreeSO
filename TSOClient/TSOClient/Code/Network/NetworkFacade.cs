using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOServiceClient.Model;
using TSOClient.Network;

namespace TSOClient.Code.Network
{
    /// <summary>
    /// Handles access to all of the network systems, service clients, city server, login events etc.
    /// </summary>
    public class NetworkFacade
    {
        public static NetworkClient Client = new NetworkClient(GlobalSettings.Default.LoginServerIP, 
            GlobalSettings.Default.LoginServerPort); 

        /// <summary>
        /// Service Client, used to interact with non realtime services such as login, city selection etc.
        /// </summary>
        public static TSOServiceClient.TSOServiceClient ServiceClient = new TSOServiceClient.TSOServiceClient();

        /// <summary>
        /// Handles the movement between network states
        /// </summary>
        public static NetworkController Controller = new NetworkController();

        /// <summary>
        /// List of cities, this is requested from the service client during login
        /// </summary>
        public static List<CityInfo> Cities;

        /// <summary>
        /// List of my avatars, this is requested from the service client during login
        /// </summary>
        public static List<AvatarInfo> Avatars;


        /// <summary>
        /// Difference between local UTC time and the server's UTC time
        /// </summary>
        public static long ClockOffset = 0;
        public static DateTime ServerTime
        {
            get
            {
                var now = new DateTime(DateTime.UtcNow.Ticks + ClockOffset);
                return now;
            }
        }

    }
}
