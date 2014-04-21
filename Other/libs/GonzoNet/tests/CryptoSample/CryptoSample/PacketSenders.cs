using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using GonzoNet;

namespace CryptoSample
{
    public class PacketSenders
    {
        //First packet sent from client to server.
        public static void SendInitialConnectPacket(NetworkClient Client, string Username)
        {
            RNGCryptoServiceProvider Random = new RNGCryptoServiceProvider();

            PacketStream InitialPacket = new PacketStream(0x01, 0);
            InitialPacket.WriteHeader();

            PacketHandlers.ClientNOnce = new byte[16];
            Random.GetNonZeroBytes(PacketHandlers.ClientNOnce);

            InitialPacket.WriteUInt16((ushort)((byte)PacketHeaders.UNENCRYPTED + 
                (PacketHandlers.ClientNOnce.ToString().Length + 1) + (Username.Length + 1)));
            InitialPacket.WriteBytes(PacketHandlers.ClientNOnce);
            InitialPacket.WritePascalString(Username);

            Client.Send(InitialPacket.ToArray());
        }
    }
}
