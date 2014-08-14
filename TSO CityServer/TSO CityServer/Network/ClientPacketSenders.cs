using System;
using System.Collections.Generic;
using System.Text;
using CityDataModel;
using GonzoNet;
using ProtocolAbstractionLibraryD;

namespace TSO_CityServer.Network
{
    public static class ClientPacketSenders
    {
        /// <summary>
        /// A new player joined the current session!
        /// </summary>
        /// <param name="Client">Client to inform about new player.</param>
        public static void SendPlayerJoinSession(NetworkClient Client, Character Player)
        {
            PacketStream JoinPacket = new PacketStream((byte)PacketType.PLAYER_JOINED_SESSION, 0);
            JoinPacket.WritePascalString(Player.Name);
            JoinPacket.WritePascalString(Player.Sex);
            JoinPacket.WritePascalString(Player.Description);
            JoinPacket.WriteInt64(Player.HeadOutfitID);
            JoinPacket.WriteInt64(Player.BodyOutfitID);
            JoinPacket.WriteInt32(Player.AppearanceType);

            Client.SendEncrypted((byte)PacketType.PLAYER_JOINED_SESSION, JoinPacket.ToArray());
        }

        /// <summary>
        /// A new player left the current session!
        /// </summary>
        /// <param name="Client">Client to inform about player leaving.</param>
        public static void SendPlayerLeftSession(NetworkClient Client, Character Player)
        {
            PacketStream JoinPacket = new PacketStream((byte)PacketType.PLAYER_LEFT_SESSION, 0);
            JoinPacket.WritePascalString(Player.Name.ToString());

            Client.SendEncrypted((byte)PacketType.PLAYER_LEFT_SESSION, JoinPacket.ToArray());
        }
    }
}
