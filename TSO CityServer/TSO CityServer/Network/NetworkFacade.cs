using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using GonzoNet;

namespace TSO_CityServer.Network
{
    public class NetworkFacade
    {
        public static SharedArrayList TransferringClients;

        static NetworkFacade()
        {
            TransferringClients = new SharedArrayList();
            PacketHandlers.Register(0x01, false, 0, new OnPacketReceive(LoginPacketHandlers.HandleClientToken));
            PacketHandlers.Register(0x64, false, 0, new OnPacketReceive(ClientPacketHandlers.HandleCharacterCreate));
        }
    }
}
