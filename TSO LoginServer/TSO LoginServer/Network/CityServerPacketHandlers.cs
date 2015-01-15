/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using GonzoNet;
using ProtocolAbstractionLibraryD;

namespace TSO_LoginServer.Network
{
    /// <summary>
    /// Contains static methods for handling incoming packets from CityServers.
    /// </summary>
    class CityServerPacketHandlers
    {
        /// <summary>
        /// A cityserver logged in!
        /// </summary>
        public static void HandleCityServerLogin(NetworkClient Client, ProcessedPacket P)
        {
            Logger.LogInfo("CityServer logged in!\r\n");

            string Name = P.ReadString();
            string Description = P.ReadString();
            string IP = P.ReadString();
            int Port = P.ReadInt32();
            CityInfoStatus Status = (CityInfoStatus)P.ReadByte();
            ulong Thumbnail = P.ReadUInt64();
            string UUID = P.ReadString();
            ulong Map = P.ReadUInt64();

            foreach (CityInfo Info in NetworkFacade.CServerListener.CityServers.GetConsumingEnumerable())
            {
                if (Info.Client == Client)
                {
                    Info.Name = Name;
                    Info.Description = Description;
                    Info.IP = IP;
                    Info.Port = Port;
                    Info.Status = Status;
                    Info.Thumbnail = Thumbnail;
                    Info.UUID = UUID;
                    Info.Map = Map;
                    Info.Client = Client;
                    Info.Online = true;

                    NetworkFacade.CServerListener.CityServers.Add(Info);

                    break;
                }

                NetworkFacade.CServerListener.CityServers.Add(Info);
            }
        }

		public static void HandlePlayerOnlineResponse(NetworkClient Client, ProcessedPacket P)
		{
			byte Result = (byte)P.ReadByte();
			string Token = P.ReadPascalString();
			//NOTE: Might have to find another way to identify a client, since two people
			//		can be on the same account from the same IP.
			string RemoteIP = P.ReadPascalString();

			PacketStream Packet;

			switch(Result)
			{
				case 0x01:
					Packet = new PacketStream((byte)PacketType.REQUEST_CITY_TOKEN, 0);
					Packet.WritePascalString(Token);

					foreach(NetworkClient PlayersClient in NetworkFacade.ClientListener.Clients)
					{
						if(PlayersClient.RemoteIP.Equals(RemoteIP, StringComparison.CurrentCultureIgnoreCase))
							PlayersClient.SendEncrypted((byte)PacketType.REQUEST_CITY_TOKEN, Packet.ToArray());
					}

					break;
				case 0x02: //Write player was already online packet!
					Packet = new PacketStream((byte)PacketType.PLAYER_ALREADY_ONLINE, 0);
					Packet.WriteByte(0x00); //Dummy

					foreach (NetworkClient PlayersClient in NetworkFacade.ClientListener.Clients)
					{
						if (PlayersClient.RemoteIP.Equals(RemoteIP, StringComparison.CurrentCultureIgnoreCase))
							PlayersClient.SendEncrypted((byte)PacketType.PLAYER_ALREADY_ONLINE, Packet.ToArray());
					}

					break;
			}
		}
    }
}