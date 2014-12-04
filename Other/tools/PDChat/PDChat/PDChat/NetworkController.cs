using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using GonzoNet;

namespace PDChat
{
    public delegate void OnReceivedCharactersDelegate();

    public class NetworkController
    {
        public static event OnReceivedCharactersDelegate OnReceivedCharacters;

        public static void Reconnect()
        {
            NetworkFacade.Client.Disconnect();

            LoginArgsContainer LoginArgs = new LoginArgsContainer();
            LoginArgs.Username = NetworkFacade.Client.ClientEncryptor.Username;
            LoginArgs.Password = Convert.ToBase64String(PlayerAccount.Hash);
            LoginArgs.Enc = NetworkFacade.Client.ClientEncryptor;

            NetworkFacade.Client = new NetworkClient(NetworkFacade.Cities[0].IP, NetworkFacade.Cities[0].Port, 
                GonzoNet.Encryption.EncryptionMode.AESCrypto);
            //THIS IS IMPORTANT - THIS NEEDS TO BE COPIED AFTER IT HAS BEEN RECREATED FOR
            //THE RECONNECTION TO WORK!
            LoginArgs.Client = NetworkFacade.Client;
            NetworkFacade.Client.OnConnected += new OnConnectedDelegate(Client_OnConnected);

            NetworkFacade.Client.Connect(LoginArgs);
        }

        /// <summary>
        /// Client connected to server successfully.
        /// </summary>
        /// <param name="LoginArgs"></param>
        public static void Client_OnConnected(LoginArgsContainer LoginArgs)
        {
            PacketSenders.SendLoginRequest(LoginArgs);
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

        public static void _OnCharacterList(NetworkClient Client, ProcessedPacket Packet)
        {
            PacketHandlers.OnCharacterInfoResponse(Packet, Client);
            OnReceivedCharacters();
        }
    }
}
