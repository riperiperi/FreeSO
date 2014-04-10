using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using GonzoNet;

namespace CryptoSample
{
    class Program
    {
        private static Listener m_Listener = new Listener();
        private static NetworkClient m_Client;

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
            GonzoNet.PacketHandlers.Register(0x02, false, 0, new OnPacketReceive(PacketHandlers.HandleServerChallenge));
            //GonzoNet requires a log output stream to function correctly. This is built in behavior.
            GonzoNet.Logger.OnMessageLogged += new MessageLoggedDelegate(Logger_OnMessageLogged);

            StaticStaticDiffieHellman.ExportKey("ClientPublic.dat", PacketHandlers.ClientKey.PublicKey);

            m_Client = new NetworkClient("127.0.0.1", 12345);
            m_Client.OnConnected += new OnConnectedDelegate(m_Client_OnConnected);

            LoginArgsContainer LoginArgs = new LoginArgsContainer();
            LoginArgs.Enc = new GonzoNet.Encryption.ARC4Encryptor("test");
            LoginArgs.Username = "test";
            LoginArgs.Password = "test";
            LoginArgs.Client = m_Client;

            PacketHandlers.GenerateClientIV();

            m_Client.Connect(LoginArgs);

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private static void m_Client_OnConnected(LoginArgsContainer LoginArgs)
        {
            PacketSenders.SendInitialConnectPacket(LoginArgs.Client, LoginArgs.Username, PacketHandlers.ClientIV);
            Console.WriteLine("Sent first packet!\r\n");
        }

        private static void RunAsServer()
        {
            GonzoNet.PacketHandlers.Register(0x01, false, 0, new OnPacketReceive(PacketHandlers.InitialClientConnect));
            //GonzoNet requires a log output stream to function correctly. This is built in behavior.
            GonzoNet.Logger.OnMessageLogged += new MessageLoggedDelegate(Logger_OnMessageLogged);

            StaticStaticDiffieHellman.ExportKey("ServerPublic.dat", PacketHandlers.ClientKey.PublicKey);

            m_Listener.Initialize(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345));

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private static void Logger_OnMessageLogged(LogMessage Msg)
        {
            Console.WriteLine("Gonzo: " + Msg.Message);
        }
    }
}
