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
using System.Net.Sockets;
using System.Security.AccessControl;
using System.Threading;
using TSOClient.Code.UI.Controls;
using TSOClient.Network;
using TSOClient.Events;
using TSOClient.Network.Events;

namespace TSOClient.Network
{
    public delegate void LoginProgressDelegate(int stage);
    public delegate void OnProgressDelegate(ProgressEvent e);
    public delegate void OnLoginStatusDelegate(LoginEvent e);

    /// <summary>
    /// Handles moving between various network states, e.g.
    /// Logging in, connecting to a city, connecting to a lot
    /// </summary>
    public class NetworkController
    {
        public event NetworkErrorDelegate OnNetworkError;
        public event OnProgressDelegate OnLoginProgress;
        public event OnLoginStatusDelegate OnLoginStatus;


        public NetworkController()
        {
        }

        public void Init(NetworkClient client){
            client.OnNetworkError += new NetworkErrorDelegate(Client_OnNetworkError);

            /** Register the various packet handlers **/
            client.On(PacketType.LOGIN_NOTIFY, new ReceivedPacketDelegate(_OnLoginNotify));
            client.On(PacketType.LOGIN_FAILURE, new ReceivedPacketDelegate(_OnLoginFailure));
            client.On(PacketType.CHARACTER_LIST, new ReceivedPacketDelegate(_OnCharacterList));
            client.On(PacketType.CITY_LIST, new ReceivedPacketDelegate(_OnCityList));
        }

        private void _OnLoginNotify(PacketStream packet)
        {
            UIPacketHandlers.OnInitLoginNotify(NetworkFacade.Client, new ProcessedPacket(packet.PacketID, false, (int)packet.Length, packet.ToArray()));
            OnLoginProgress(new ProgressEvent(false, EventCodes.PROGRESS_UPDATE) { Done = 2, Total = 4 });
        }

        private void _OnLoginFailure(PacketStream packet)
        {
            UIPacketHandlers.OnLoginFailResponse(ref NetworkFacade.Client, new ProcessedPacket(packet.PacketID, false, (int)packet.Length, packet.ToArray()));
            OnLoginStatus(new LoginEvent(EventCodes.LOGIN_RESULT) { Success = false });
        }

        private void _OnCharacterList(PacketStream packet)
        {
            OnLoginProgress(new ProgressEvent(false, EventCodes.PROGRESS_UPDATE) { Done = 3, Total = 4 });
            UIPacketHandlers.OnCharacterInfoResponse(new ProcessedPacket(packet.PacketID, true, (int)packet.Length, packet.ToArray()), NetworkFacade.Client);
        }

        private void _OnCityList(PacketStream packet)
        {
            UIPacketHandlers.OnCityInfoResponse(new ProcessedPacket(packet.PacketID, true, (int)packet.Length, packet.ToArray()));
            OnLoginProgress(new ProgressEvent(false, EventCodes.PROGRESS_UPDATE) { Done = 4, Total = 4 });
            OnLoginStatus(new LoginEvent(EventCodes.LOGIN_RESULT) { Success = true });
        }

        /// <summary>
        /// Authenticate with the service client to get a token,
        /// Then get info about avatars & cities
        /// </summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public void InitialConnect(string username, string password)
        {
            var client = NetworkFacade.Client;
            client.Connect(username, password);


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
        }

        private void Client_OnNetworkError(SocketException Exception)
        {
            OnNetworkError(Exception);
        }

        /// <summary>
        /// Logout of the game & service client
        /// </summary>
        public void Logout()
        {

        }
    }
}
