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
using GonzoNet;
using TSOClient.VM;
using ProtocolAbstractionLibraryD;

namespace TSOClient.Network
{
    /// <summary>
    /// Handles access to all of the network systems, service clients, city server, login events etc.
    /// </summary>
    public class NetworkFacade
    {
        public static NetworkClient Client;

        /// <summary>
        /// Handles the movement between network states
        /// </summary>
        public static NetworkController Controller;

        /// <summary>
        /// List of cities, this is requested from the service client during login
        /// </summary>
        public static List<CityInfo> Cities = new List<CityInfo>();

        /// <summary>
        /// List of my avatars, this is requested from the service client during login
        /// </summary>
        public static List<Sim> Avatars = new List<Sim>();

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

        static NetworkFacade()
        {
            Client = new NetworkClient(GlobalSettings.Default.LoginServerIP, GlobalSettings.Default.LoginServerPort);
            Client.OnConnected += new OnConnectedDelegate(UIPacketSenders.SendLoginRequest);
            Controller = new NetworkController();
            Controller.Init(Client);

            //PacketHandlers.Init();
            PacketHandlers.Register((byte)PacketType.LOGIN_NOTIFY, false, 2, new OnPacketReceive(Controller._OnLoginNotify));
            PacketHandlers.Register((byte)PacketType.LOGIN_FAILURE, false, 2, new OnPacketReceive(Controller._OnLoginFailure));
            PacketHandlers.Register((byte)PacketType.CHARACTER_LIST, true, 0, new OnPacketReceive(Controller._OnCharacterList));
            PacketHandlers.Register((byte)PacketType.CITY_LIST, true, 0, new OnPacketReceive(Controller._OnCityList));
            PacketHandlers.Register((byte)PacketType.CHARACTER_CREATION_STATUS, true, 0, new OnPacketReceive(Controller._OnCharacterCreationProgress));

            PacketHandlers.Register((byte)PacketType.CHARACTER_CREATE_CITY, true, 0, new OnPacketReceive(Controller._OnCharacterCreationStatus));
            //TODO: Register handler for 0x65 - character city creation failed...
            PacketHandlers.Register((byte)PacketType.REQUEST_CITY_TOKEN, true, 0, new OnPacketReceive(Controller._OnCityToken));
            PacketHandlers.Register((byte)PacketType.CITY_TOKEN, true, 0, new OnPacketReceive(Controller._OnCityTokenResponse));
        }
    }
}
