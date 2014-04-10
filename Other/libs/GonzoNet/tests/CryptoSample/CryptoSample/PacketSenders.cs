using System;
using System.Collections.Generic;
using System.Text;
using GonzoNet;

namespace CryptoSample
{
    public class PacketSenders
    {
        //First packet sent from client to server.
        public static void SendInitialConnectPacket(NetworkClient Client, string Username, byte[] InitializationVector)
        {
            PacketStream InitialPacket = new PacketStream(0x01, 0);
            InitialPacket.WriteHeader();

            InitialPacket.WriteUInt16((ushort)((byte)PacketHeaders.UNENCRYPTED + 
                (PacketHandlers.ClientNOnce.ToString().Length + 1) + (Username.Length + 1)));
            InitialPacket.WritePascalString(PacketHandlers.ClientNOnce.ToString());
            InitialPacket.WritePascalString(Username);

            Client.Send(InitialPacket.ToArray());
        }
    }
}
