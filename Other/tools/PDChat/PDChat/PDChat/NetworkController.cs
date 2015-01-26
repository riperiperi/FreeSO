using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using PDChat.Sims;
using GonzoNet;

namespace PDChat
{
    public delegate void OnReceivedCharactersDelegate();
    public delegate void OnPlayerJoinedSessionDelegate(Sim Avatar);
    public delegate void OnReceivedMessageDelegate(string Msg);

    public class NetworkController
    {
        public static event OnReceivedCharactersDelegate OnReceivedCharacters;
        public static event OnPlayerJoinedSessionDelegate OnPlayerJoinedSession;
        public static event OnReceivedMessageDelegate OnReceivedMessage;

        private static bool IsReconnect = false;

        /// <summary>
        /// Reconnects to city server.
        /// </summary>
        private static void Reconnect()
        {
            LoginArgsContainer LoginArgs = new LoginArgsContainer();
            LoginArgs.Username = NetworkFacade.Client.ClientEncryptor.Username;
            LoginArgs.Password = Convert.ToBase64String(PlayerAccount.Hash);
            LoginArgs.Enc = NetworkFacade.Client.ClientEncryptor;

            lock (NetworkFacade.Client)
            {
                NetworkFacade.Client = new NetworkClient(NetworkFacade.Cities[0].IP, NetworkFacade.Cities[0].Port,
                    GonzoNet.Encryption.EncryptionMode.AESCrypto);
                //THIS IS IMPORTANT - THIS NEEDS TO BE COPIED AFTER IT HAS BEEN RECREATED FOR
                //THE RECONNECTION TO WORK!
                LoginArgs.Client = NetworkFacade.Client;
                NetworkFacade.Client.OnConnected += new OnConnectedDelegate(Client_OnConnected);
                GonzoNet.Logger.OnMessageLogged += new MessageLoggedDelegate(Logger_OnMessageLogged);

                IsReconnect = true;
                NetworkFacade.Client.Connect(LoginArgs);
            }
        }

        /// <summary>
        /// Messages logged by GonzoNet.
        /// </summary>
        /// <param name="Msg">The message that was logged.</param>
        public static void Logger_OnMessageLogged(LogMessage Msg)
        {
            Debug.WriteLine(Msg.Message);
        }

        /// <summary>
        /// Client connected to server successfully.
        /// </summary>
        /// <param name="LoginArgs"></param>
        public static void Client_OnConnected(LoginArgsContainer LoginArgs)
        {
            if (!IsReconnect)
                PacketSenders.SendLoginRequest(LoginArgs);
            else
                PacketSenders.SendLoginRequestCity(LoginArgs);
        }

        public static void _OnLoginNotify(NetworkClient Client, ProcessedPacket Packet)
        {
            PacketHandlers.HandleLoginNotify(Client, Packet);
        }

        public static void _OnLoginSuccess(NetworkClient Client, ProcessedPacket Packet)
        {
            PacketHandlers.OnLoginSuccessResponse(ref Client, Packet);
        }

        public static void _OnLoginFailure(NetworkClient Client, ProcessedPacket Packet)
        {
            Client.Disconnect();
            MessageBox.Show("Invalid credentials!");
        }

        public static void _OnInvalidVersion(NetworkClient Client, ProcessedPacket Packet)
        {
            Client.Disconnect();
            MessageBox.Show("Invalid version: " + GlobalSettings.Default.ClientVersion + "!");
        }

        /// <summary>
        /// LoginServer sent information about the player's characters.
        /// </summary>
        /// <param name="Packet">The packet that was received.</param>
        public static void _OnCharacterList(NetworkClient Client, ProcessedPacket Packet)
        {
            PacketHandlers.OnCharacterInfoResponse(Packet, Client);
            OnReceivedCharacters();
        }

        /// <summary>
        /// LoginServer sent information about connected cities.
        /// </summary>
        /// <param name="Packet">The packet that was received.</param>
        public static void _OnCityList(NetworkClient Client, ProcessedPacket Packet)
        {
            PacketHandlers.OnCityInfoResponse(Packet);
        }

        /// <summary>
        /// Received from the LoginServer in response to a CITY_TOKEN_REQUEST packet.
        /// </summary>
        public static void _OnCityTokenRequest(NetworkClient Client, ProcessedPacket Packet)
        {
            PacketHandlers.OnCityTokenRequest(Client, Packet);
            Reconnect();
        }

        /// <summary>
        /// Received initial packet from CityServer.
        /// </summary>
        public static void _OnLoginNotifyCity(NetworkClient Client, ProcessedPacket Packet)
        {
            PacketHandlers.OnLoginNotifyCity(Packet, Client);
        }

        public static void _OnLoginSuccessCity(NetworkClient Client, ProcessedPacket Packet)
        {
            Debug.WriteLine("Received LoginSuccessCity!");
            PacketSenders.SendCityToken(Client);
        }

        /// <summary>
        /// Received from the CityServer in response to a CITY_TOKEN packet.
        /// </summary>
        public static void _OnCityTokenResponse(NetworkClient Client, ProcessedPacket Packet)
        {
            ProtocolAbstractionLibraryD.CityTransferStatus Status = PacketHandlers.OnCityTokenResponse(Client, Packet);

            //TODO: Do something if status wasn't succesful.
        }

        public static void _OnPlayerJoinedSession(NetworkClient Client, ProcessedPacket Packet)
        {
            Sim Avatar = PacketHandlers.OnPlayerJoinedSession(Packet);
            OnPlayerJoinedSession(Avatar);
        }

        public static void _OnReceivedMessage(NetworkClient Client, ProcessedPacket Packet)
        {
            string Message = PacketHandlers.OnReceivedMessage(Packet);
            OnReceivedMessage(Message);
        }
    }
}
