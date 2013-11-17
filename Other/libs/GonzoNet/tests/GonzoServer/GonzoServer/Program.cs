using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using GonzoNet;
using GonzoNet.Encryption;

namespace GonzoServer
{
    class Program
    {
        private static Listener m_Listener;

        static void Main(string[] args)
        {
            m_Listener = new Listener();
            m_Listener.Initialize(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 1800));
            PacketHandlers.Register(0x00, 0, new OnPacketReceive(OnUnencryptedPacket));
            PacketHandlers.Register(0x01, 0, new OnPacketReceive(OnEncryptedPacket));
        }

        private static void OnUnencryptedPacket(NetworkClient Client, PacketStream Packet)
        {
            ProcessedPacket PPacket = new ProcessedPacket(0x00, false, (ushort)Packet.Length, Client.ClientEncryptor,
                Packet.ToArray());

            Handlers.ReceivedUnEncryptedPacket(Client, PPacket);
        }

        private static void OnEncryptedPacket(NetworkClient Client, PacketStream Packet)
        {
            ProcessedPacket PPacket = new ProcessedPacket(0x01, true, (ushort)Packet.Length, Client.ClientEncryptor,
                Packet.ToArray());

            Handlers.ReceivedEncryptedPacket(Client, PPacket);
        }
    }
}
