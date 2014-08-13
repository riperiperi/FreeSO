/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO CityServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

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

        //Encryption
        public static ECDiffieHellmanCng ServerPrivateKey = new ECDiffieHellmanCng();
        public static byte[] ServerPublicKey = ServerPrivateKey.PublicKey.ToByteArray();

        public static Session CurrentSession = new Session();

        static NetworkFacade()
        {
            TransferringClients = new SharedArrayList();

            //INTERNAL PACKETS SENT BY LOGINSERVER
            PacketHandlers.Register(0x01, false, 0, new OnPacketReceive(LoginPacketHandlers.HandleClientToken));
            PacketHandlers.Register(0x02, false, 0, new OnPacketReceive(LoginPacketHandlers.HandleCharacterRetirement));

            //PACKETS RECEIVED BY CLIENT
            PacketHandlers.Register((byte)PacketType.LOGIN_REQUEST_CITY, false, 0, new OnPacketReceive(ClientPacketHandlers.InitialClientConnect));
            PacketHandlers.Register((byte)PacketType.CHALLENGE_RESPONSE, true, 0, new OnPacketReceive(ClientPacketHandlers.HandleChallengeResponse));
            PacketHandlers.Register((byte)PacketType.CHARACTER_CREATE_CITY, true, 0, new OnPacketReceive(ClientPacketHandlers.HandleCharacterCreate));
            PacketHandlers.Register((byte)PacketType.CITY_TOKEN, true, 0, new OnPacketReceive(ClientPacketHandlers.HandleCityToken));
        }
    }
}
