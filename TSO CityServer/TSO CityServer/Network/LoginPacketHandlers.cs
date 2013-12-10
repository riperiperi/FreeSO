using System;
using System.Collections.Generic;
using System.Text;
using GonzoNet;

namespace TSO_CityServer.Network
{
    public class LoginPacketHandlers
    {
        public static void HandleClientToken(NetworkClient Client, ProcessedPacket P)
        {
            ClientToken Token = new ClientToken();
            Token.ClientIP = P.ReadPascalString();
            Token.Token = P.ReadPascalString();
            
            NetworkFacade.TransferringClients.AddItem(Token);
        }
    }
}
