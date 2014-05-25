using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Threading;
using System.Security.Cryptography;
using GonzoNet;
using GonzoNet.Encryption;

namespace CryptoSample
{
    class Program
    {
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

            NetworkFacade.Client = new NetworkClient("127.0.0.1", 12345);
            NetworkFacade.Client.OnConnected += new OnConnectedDelegate(m_Client_OnConnected);

            LoginArgsContainer LoginArgs = new LoginArgsContainer();
            LoginArgs.Enc = new AESEncryptor("test");
            LoginArgs.Username = "test";
            LoginArgs.Password = "test";
            LoginArgs.Client = NetworkFacade.Client;

            SaltedHash Hash = new SaltedHash(new SHA512Managed(), LoginArgs.Username.Length);
            PacketHandlers.PasswordHash = Hash.ComputePasswordHash(LoginArgs.Username, LoginArgs.Password);

            NetworkFacade.Client.Connect(LoginArgs);

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        private static void m_Client_OnConnected(LoginArgsContainer LoginArgs)
        {
            PacketSenders.SendInitialConnectPacket(LoginArgs.Client, LoginArgs.Username);
            Console.WriteLine("Sent first packet!\r\n");
        }

        private static void RunAsServer()
        {
            GonzoNet.PacketHandlers.Register(0x01, false, 0, new OnPacketReceive(PacketHandlers.InitialClientConnect));
            GonzoNet.PacketHandlers.Register(0x03, true, 0, new OnPacketReceive(PacketHandlers.HandleChallengeResponse));
            //GonzoNet requires a log output stream to function correctly. This is built in behavior.
            GonzoNet.Logger.OnMessageLogged += new MessageLoggedDelegate(Logger_OnMessageLogged);

            NetworkFacade.Listener.Initialize(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 12345));

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
