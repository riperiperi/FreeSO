/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using TSOServiceClient.Model;
using TSOClient.Network;
using TSOClient.VM;

namespace TSOClient.Network
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
        public static List<Sim> Avatars = new List<Sim>();

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
