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
