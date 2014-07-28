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
using GonzoNet.Encryption;
using System.Security.Cryptography;
using System.Threading;
using TSOClient.Code.UI.Controls;
using TSOClient.Events;
using TSOClient.Network.Events;
using GonzoNet;
using ProtocolAbstractionLibraryD;
using LogThis;

namespace TSOClient.Network
{
    public delegate void LoginProgressDelegate(int stage);
    public delegate void OnProgressDelegate(ProgressEvent e);
    public delegate void OnLoginStatusDelegate(LoginEvent e);

    public delegate void OnLoginNotifyCityDelegate();
    public delegate void OnCharacterCreationProgressDelegate(CharacterCreationStatus CCStatus);
    public delegate void OnCharacterCreationStatusDelegate(CharacterCreationStatus CCStatus);
    public delegate void OnLoginSuccessCityDelegate();
    public delegate void OnLoginFailureCityDelegate();
    public delegate void OnCityTokenDelegate(CityInfo SelectedCity);
    public delegate void OnCityTransferProgressDelegate(CityTransferStatus e);
    public delegate void OnCharacterRetirementDelegate(string GUID);

    /// <summary>
    /// Handles moving between various network states, e.g.
    /// Logging in, connecting to a city, connecting to a lot
    /// </summary>
    public class NetworkController
    {
        public event NetworkErrorDelegate OnNetworkError;
        public event OnProgressDelegate OnLoginProgress;
        public event OnLoginStatusDelegate OnLoginStatus;

        public event OnLoginNotifyCityDelegate OnLoginNotifyCity;
        public event OnCharacterCreationProgressDelegate OnCharacterCreationProgress;
        public event OnCharacterCreationStatusDelegate OnCharacterCreationStatus;
        public event OnLoginSuccessCityDelegate OnLoginSuccessCity;
        public event OnLoginFailureCityDelegate OnLoginFailureCity;
        public event OnCityTokenDelegate OnCityToken;
        public event OnCityTransferProgressDelegate OnCityTransferProgress;
        public event OnCharacterRetirementDelegate OnCharacterRetirement;

        public NetworkController()
        {
        }

        public void Init(NetworkClient client)
        {
            client.OnNetworkError += new NetworkErrorDelegate(Client_OnNetworkError);
            GonzoNet.Logger.OnMessageLogged += new GonzoNet.MessageLoggedDelegate(Logger_OnMessageLogged);
            ProtocolAbstractionLibraryD.Logger.OnMessageLogged += new 
                ProtocolAbstractionLibraryD.MessageLoggedDelegate(Logger_OnMessageLogged);
        }

        #region Log Sink

        private void Logger_OnMessageLogged(GonzoNet.LogMessage Msg)
        {
            Log.LogThis(Msg.Message, (eloglevel)Msg.Level);
        }

        private void Logger_OnMessageLogged(ProtocolAbstractionLibraryD.LogMessage Msg)
        {
            switch (Msg.Level)
            {
                case ProtocolAbstractionLibraryD.LogLevel.error:
                    Log.LogThis(Msg.Message, eloglevel.error);
                    break;
                case ProtocolAbstractionLibraryD.LogLevel.info:
                    Log.LogThis(Msg.Message, eloglevel.info);
                    break;
                case ProtocolAbstractionLibraryD.LogLevel.warn:
                    Log.LogThis(Msg.Message, eloglevel.warn);
                    break;
            }
        }

        #endregion

        public void _OnLoginNotify(NetworkClient Client, ProcessedPacket packet)
        {
            UIPacketHandlers.OnLoginNotify(NetworkFacade.Client, packet);
            OnLoginProgress(new ProgressEvent(EventCodes.PROGRESS_UPDATE) { Done = 2, Total = 5 });
        }

        public void _OnLoginFailure(NetworkClient Client, ProcessedPacket packet)
        {
            UIPacketHandlers.OnLoginFailResponse(ref NetworkFacade.Client, packet);
            OnLoginStatus(new LoginEvent(EventCodes.LOGIN_RESULT) { Success = false, VersionOK = true });
        }

        public void _OnLoginSuccess(NetworkClient Client, ProcessedPacket packet)
        {
            UIPacketHandlers.OnLoginSuccessResponse(ref NetworkFacade.Client, packet);
            OnLoginProgress(new ProgressEvent(EventCodes.PROGRESS_UPDATE) { Done = 3, Total = 5 });
        }

        public void _OnInvalidVersion(NetworkClient Client, ProcessedPacket packet)
        {
            UIPacketHandlers.OnInvalidVersionResponse(ref NetworkFacade.Client, packet);
            OnLoginStatus(new LoginEvent(EventCodes.LOGIN_RESULT) { Success = false, VersionOK = false });
        }

        /// <summary>
        /// Received list of characters for account from login server.
        /// </summary>
        public void _OnCharacterList(NetworkClient Client, ProcessedPacket packet)
        {
            OnLoginProgress(new ProgressEvent(EventCodes.PROGRESS_UPDATE) { Done = 4, Total = 5 });
            UIPacketHandlers.OnCharacterInfoResponse(packet, NetworkFacade.Client);
        }

        /// <summary>
        /// Received a list of available cities from the login server.
        /// </summary>
        public void _OnCityList(NetworkClient Client, ProcessedPacket packet)
        {
            UIPacketHandlers.OnCityInfoResponse(packet);
            OnLoginProgress(new ProgressEvent(EventCodes.PROGRESS_UPDATE) { Done = 5, Total = 5 });
            OnLoginStatus(new LoginEvent(EventCodes.LOGIN_RESULT) { Success = true });
        }

        /// <summary>
        /// Progressing to city server (received from login server).
        /// </summary>
        public void _OnCharacterCreationProgress(NetworkClient Client, ProcessedPacket Packet)
        {
            Log.LogThis("Received OnCharacterCreationProgress!", eloglevel.info);

            CharacterCreationStatus CCStatus = UIPacketHandlers.OnCharacterCreationProgress(Client, Packet);
            OnCharacterCreationProgress(CCStatus);
        }

        public void _OnLoginNotifyCity(NetworkClient Client, ProcessedPacket packet)
        {
            UIPacketHandlers.OnLoginNotifyCity(Client, packet);
            OnLoginNotifyCity();
        }

        public void _OnLoginSuccessCity(NetworkClient Client, ProcessedPacket Packet)
        {
            Log.LogThis("Received OnLoginSuccessCity!", eloglevel.info);

            //No need for handler - only contains dummy byte.
            OnLoginSuccessCity();
        }

        public void _OnLoginFailureCity(NetworkClient Client, ProcessedPacket Packet)
        {
            Log.LogThis("Received OnLoginFailureCity!", eloglevel.info);

            //No need for a handler for this packet - only sent on invalid challenge response.
            OnLoginFailureCity();
        }

        public void _OnCharacterCreationStatus(NetworkClient Client, ProcessedPacket Packet)
        {
            CharacterCreationStatus CCStatus = UIPacketHandlers.OnCharacterCreationStatus(Client, Packet);
            OnCharacterCreationStatus(CCStatus);
        }

        /// <summary>
        /// Received token from login server.
        /// </summary>
        public void _OnCityToken(NetworkClient Client, ProcessedPacket Packet)
        {
            UIPacketHandlers.OnCityToken(Client, Packet);
            OnCityToken(PlayerAccount.CurrentlyActiveSim.ResidingCity);
        }

        /// <summary>
        /// Response from city server.
        /// </summary>
        public void _OnCityTokenResponse(NetworkClient Client, ProcessedPacket Packet)
        {
            Log.LogThis("Received OnCityTokenResponse!", eloglevel.info);

            CityTransferStatus Status = UIPacketHandlers.OnCityTokenResponse(Client, Packet);
            OnCityTransferProgress(Status);
        }

        public void _OnRetireCharacterStatus(NetworkClient Client, ProcessedPacket Packet)
        {
            string GUID = UIPacketHandlers.OnCharacterRetirement(Client, Packet);
            OnCharacterRetirement(GUID);
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
            LoginArgsContainer Args = new LoginArgsContainer();
            Args.Username = username;
            Args.Password = password;

            //Doing the encryption this way eliminates the need to send key across the wire! :D
            SaltedHash Hash = new SaltedHash(new SHA512Managed(), Args.Username.Length);
            byte[] HashBuf = Hash.ComputePasswordHash(Args.Username, Args.Password);

            Args.Enc = new GonzoNet.Encryption.AESEncryptor(Convert.ToBase64String(HashBuf));
            Args.Client = client;

            client.Connect(Args);
        }
        
        /// <summary>
        /// Reconnects to a CityServer.
        /// </summary>
        public void Reconnect(ref NetworkClient Client, CityInfo SelectedCity, LoginArgsContainer LoginArgs)
        {
            Client.Disconnect();
            Client.Connect(LoginArgs);
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
