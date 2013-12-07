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
            PacketHandlers.Register(0x00, false, 0, new OnPacketReceive(Handlers.ReceivedUnEncryptedPacket));
            PacketHandlers.Register(0x01, true, 0, new OnPacketReceive(Handlers.ReceivedEncryptedPacket));
        }
    }
}
