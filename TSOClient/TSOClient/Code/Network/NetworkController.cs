using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace TSOClient.Code.Network
{
    public delegate void LoginProgressDelegate(int stage);


    /// <summary>
    /// Handles moving between various network states, e.g.
    /// 
    /// Logging in, connecting to a city, connecting to a lot
    /// </summary>
    public class NetworkController
    {
        public NetworkController()
        {
        }

        /// <summary>
        /// Authenticate with the service client to get a token,
        /// Then get info about avatars & cities
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void InitialConnect(string username, string password, LoginProgressDelegate progressDelegate)
        {
            progressDelegate(1);

            var authResult = NetworkFacade.ServiceClient.Authenticate(new TSOServiceClient.Model.AuthRequest {
                Username = username,
                Password = password
            });

            if (authResult.Status == TSOServiceClient.Model.TSOServiceStatus.Error)
            {
                //TODO: Handle error
                return;
            }

            /* Use the session start time as a rough guide for server clock offset, we will do a real
             * clock sync later in the game **/
            NetworkFacade.ClockOffset = authResult.Body.SessionStart.Ticks - DateTime.UtcNow.Ticks;
            progressDelegate(2);

            /**
             * Get city info & store it
             */
            var cityList = NetworkFacade.ServiceClient.GetCityList();
            if (cityList.Status == TSOServiceClient.Model.TSOServiceStatus.Error)
            {
                //TODO: Handle error
                return;
            }
            NetworkFacade.Cities = cityList.Body.Cities;
            progressDelegate(3);

            /**
             * Get my avatars
             */


            progressDelegate(4);
        }

        /// <summary>
        /// Logout of the game & service client
        /// </summary>
        public void Logout()
        {

        }

    }
}
