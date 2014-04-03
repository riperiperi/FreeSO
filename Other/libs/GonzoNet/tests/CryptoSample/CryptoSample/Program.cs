using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using GonzoNet;

namespace CryptoSample
{
    class Program
    {
        private static Listener m_Listener = new Listener();

        static void Main(string[] args)
        {
            Console.WriteLine("Run as server? (Y/N)");
            string Answer = Console.ReadLine();

            switch (Answer)
            {
                case "Y":
                    RunAsServer();
                    break;
                case "y":
                    RunAsServer();
                    break;
                case "N":
                    RunAsClient();
                    break;
                case "n":
                    RunAsClient();
                    break;
            }
        }

        private static void RunAsClient()
        {
            throw new NotImplementedException();
        }

        private static void RunAsServer()
        {
            m_Listener.Initialize(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345));
            GonzoNet.PacketHandlers.Register(0x01, false, 0, new OnPacketReceive(PacketHandlers.InitialClientConnect));
        }
    }
}
