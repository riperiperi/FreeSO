using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TSOServiceClient.Model;
using TSOClient.Network;
using TSOClient.VM;

namespace TSOClient.Code.Network
{
    /// <summary>
    /// Handles access to all of the network systems, service clients, city server, login events etc.
    /// </summary>
    public class NetworkFacade
    {
        //The loginscreen waits for this to become signaled in order to progress.
        public static ManualResetEvent LoginWait = new ManualResetEvent(false);
        //Called to update login progress.
        public static event LoginProgressDelegate LoginProgress;

        //Set to true if login was successful.
        public static bool LoginOK = false;

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
        public static List<Sim> Avatars;

        public static void UpdateLoginProgress(int Stage)
        {
            LoginProgress(Stage);
        }

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
