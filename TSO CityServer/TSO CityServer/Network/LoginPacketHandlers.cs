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
            try
            {
                ClientToken Token = new ClientToken();
                Token.AccountID = P.ReadInt32();
                Token.ClientIP = P.ReadPascalString();
                Token.CharacterGUID = P.ReadPascalString();
                Token.Token = P.ReadPascalString();

                NetworkFacade.TransferringClients.AddItem(Token);
            }
            catch (Exception E)
            {
                Logger.LogDebug("Exception in HandleClientToken: " + E.ToString());
            }
        }
    }
}
