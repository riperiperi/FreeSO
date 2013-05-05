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
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Threading;
using TSOClient.Code.UI.Controls;
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
        public event NetworkErrorDelegate OnNetworkError;

        public NetworkController()
        {
        }

        /// <summary>
        /// Authenticate with the service client to get a token,
        /// Then get info about avatars & cities
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void InitialConnect(string username, string password)
        {
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

            NetworkFacade.Client.OnNetworkError += new NetworkErrorDelegate(Client_OnNetworkError);
            NetworkFacade.Client.Connect(username, password);
            NetworkFacade.UpdateLoginProgress(1);

            NetworkFacade.Client.OnReceivedData += new TSOClient.Network.ReceivedPacketDelegate(
                Client_OnReceivedData);
        }

        private void Client_OnNetworkError(SocketException Exception)
        {
            OnNetworkError(Exception);
        }

        private void Client_OnReceivedData(TSOClient.Network.PacketStream Packet)
        {
            switch (Packet.PacketID)
            {
                case 0x01:
                    UIPacketHandlers.OnInitLoginNotify(NetworkFacade.Client, Packet);
                    NetworkFacade.UpdateLoginProgress(2);
                    break;
                case 0x02:
                    NetworkFacade.LoginWait.Set();
                    UIPacketHandlers.OnLoginFailResponse(ref NetworkFacade.Client, Packet);
                    break;
                case 0x05:
                    NetworkFacade.LoginOK = true;
                    NetworkFacade.LoginWait.Set();

                    NetworkFacade.UpdateLoginProgress(3);

                    UIPacketHandlers.OnCharacterInfoResponse(NetworkFacade.Client, Packet);
                    NetworkFacade.Avatars = PlayerAccount.Sims;
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
