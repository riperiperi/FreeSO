using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using System.Security.Cryptography;
using GonzoNet;
using ProtocolAbstractionLibraryD;

namespace TSO_CityServer.Network
{
    public class NetworkFacade
    {
        public static SharedArrayList TransferringClients;
        public static Listener NetworkListener;

        public static ECDiffieHellmanCng ServerPrivateKey = new ECDiffieHellmanCng();
        public static byte[] ServerPublicKey = ServerPrivateKey.PublicKey.ToByteArray();

        static NetworkFacade()
        {
            TransferringClients = new SharedArrayList();

            //INTERNAL PACKETS SENT BY LOGINSERVER
            PacketHandlers.Register(0x01, false, 0, new OnPacketReceive(LoginPacketHandlers.HandleClientToken));
            PacketHandlers.Register(0x02, false, 0, new OnPacketReceive(LoginPacketHandlers.HandleCharacterRetirement));

            //PACKETS RECEIVED BY CLIENT
            PacketHandlers.Register((byte)PacketType.CHARACTER_CREATE_CITY, false, 0, new OnPacketReceive(ClientPacketHandlers.HandleCharacterCreate));
            PacketHandlers.Register((byte)PacketType.CITY_TOKEN, false, 0, new OnPacketReceive(ClientPacketHandlers.HandleCityToken));
        }
    }
}
