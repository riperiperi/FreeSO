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
            JoinPacket.WritePascalString(Player.GUID.ToString());
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
            JoinPacket.WritePascalString(Player.GUID.ToString());

            Client.SendEncrypted((byte)PacketType.PLAYER_LEFT_SESSION, JoinPacket.ToArray());
        }

        /// <summary>
        /// Player received a letter from another player.
        /// </summary>
        /// <param name="Client">Client of receiving player.</param>
        /// <param name="Subject">Letter's subject.</param>
        /// <param name="Msg">Letter's body.</param>
        /// <param name="LetterFrom">Name of player sending the letter.</param>
        public static void SendPlayerReceivedLetter(NetworkClient Client, string Subject, string Msg, string LetterFrom)
        {
            PacketStream Packet = new PacketStream((byte)PacketType.PLAYER_RECV_LETTER, 0);
            Packet.WritePascalString(LetterFrom);
            Packet.WritePascalString(Subject);
            Packet.WritePascalString(Msg);

            Client.SendEncrypted((byte)PacketType.PLAYER_RECV_LETTER, Packet.ToArray());
        }
    }
}
