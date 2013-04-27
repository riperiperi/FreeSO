using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using TSOClient.Network;

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
        public bool InitialConnect(string username, string password, LoginProgressDelegate progressDelegate)
        {
            progressDelegate(1);

            /*var authResult = NetworkFacade.ServiceClient.Authenticate(new TSOServiceClient.Model.AuthRequest {
                Username = username,
                Password = password
            });

            if (authResult.Status == TSOServiceClient.Model.TSOServiceStatus.Error)
            {
                //TODO: Handle error
                return false;
            }*/

            /* Use the session start time as a rough guide for server clock offset, we will do a real
             * clock sync later in the game **/
            //NetworkFacade.ClockOffset = authResult.Body.SessionStart.Ticks - DateTime.UtcNow.Ticks;
            //progressDelegate(2);

            /**
             * Get city info & store it
             */
            /*var cityList = NetworkFacade.ServiceClient.GetCityList();
            if (cityList.Status == TSOServiceClient.Model.TSOServiceStatus.Error)
            {
                //TODO: Handle error
                return false;
            }
            NetworkFacade.Cities = cityList.Body.Cities;
            progressDelegate(3);*/

            /**
             * Get my avatars
             */
            /*var avatarList = NetworkFacade.ServiceClient.GetAvatarList();
            if (avatarList.Status == TSOServiceClient.Model.TSOServiceStatus.Error)
            {
                //TODO: Handle error
                return false;
            }
            NetworkFacade.Avatars = avatarList.Body.Avatars;
            progressDelegate(4);

            foreach (var city in NetworkFacade.Cities)
            {
                var avatarInCity = NetworkFacade.Avatars.FirstOrDefault(x => x.CityId == city.ID);
                if (avatarInCity != null)
                {
                    city.Status = TSOServiceClient.Model.CityInfoStatus.Reserved;
                }
            }*/

            NetworkFacade.Client.Connect(username, password);
            NetworkFacade.Client.OnReceivedData += new TSOClient.Network.ReceivedPacketDelegate(
                Client_OnReceivedData);

            return true;
        }

        private void Client_OnReceivedData(TSOClient.Network.PacketStream Packet)
        {
            switch (Packet.PacketID)
            {
                case 0x01:
                    UIPacketHandlers.OnInitLoginNotify(NetworkFacade.Client, Packet);
                    break;
                case 0x02:
                    UIPacketHandlers.OnLoginFailResponse(ref NetworkFacade.Client, Packet);
                    break;
            }
        }

        /// <summary>
        /// Logout of the game & service client
        /// </summary>
        public void Logout()
        {

        }
    }
}
