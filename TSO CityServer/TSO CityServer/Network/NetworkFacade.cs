/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
using GonzoNet.Concurrency;
using ProtocolAbstractionLibraryD;
using TSO_CityServer.Terrain;

namespace TSO_CityServer.Network
{
	public class NetworkFacade
	{
		public static BlockingCollection<ClientToken> TransferringClients;
		public static Listener NetworkListener;
		public static DateTime StartTime;		//When was server started?

		//Encryption
		public static ECDiffieHellmanCng ServerPrivateKey = new ECDiffieHellmanCng();
		public static byte[] ServerPublicKey = ServerPrivateKey.PublicKey.ToByteArray();

		public static Session CurrentSession = new Session();
		public static Terrain.Terrain CurrentTerrain = new Terrain.Terrain();

		public const int LOT_COST = 2000; //Hardcoded for now...

		/// <summary>
		/// Thread-safe method for getting client tokens.
		/// </summary>
		/// <param name="Tok">A RemoteIP corresponding to a token in TransferringClients.</param>
		/// <returns>A ClientToken instance if found, null otherwise.</returns>
		public static ClientToken GetClientToken(string RemoteIP)
		{
			lock(TransferringClients)
			{
				foreach(ClientToken Token in TransferringClients)
				{
					if (RemoteIP.Equals(Token.ClientIP, StringComparison.CurrentCultureIgnoreCase))
						return Token;
				}
			}

			return null;
		}

		/// <summary>
		/// Thread-safe method for getting client tokens.
		/// </summary>
		/// <param name="Tok">A GUID corresponding to a token in TransferringClients.</param>
		/// <returns>A ClientToken instance if found, null otherwise.</returns>
		public static ClientToken GetClientToken(Guid Tok)
		{
			lock (TransferringClients)
			{
				foreach (ClientToken Token in TransferringClients)
				{
					if (Tok.ToString().Equals(Token.Token, StringComparison.CurrentCultureIgnoreCase))
						return Token;
				}
			}

			return null;
		}

		static NetworkFacade()
		{
			TransferringClients = new BlockingCollection<ClientToken>();
			StartTime = DateTime.Now;

			//INTERNAL PACKETS SENT BY LOGINSERVER
			PacketHandlers.Register(0x01, false, 0, new OnPacketReceive(LoginPacketHandlers.HandleClientToken));
			PacketHandlers.Register(0x02, false, 0, new OnPacketReceive(LoginPacketHandlers.HandleCharacterRetirement));

			//PACKETS RECEIVED BY CLIENT
			PacketHandlers.Register((byte)PacketType.LOGIN_REQUEST_CITY, false, 0, new OnPacketReceive(ClientPacketHandlers.InitialClientConnect));
			PacketHandlers.Register((byte)PacketType.CHALLENGE_RESPONSE, true, 0, new OnPacketReceive(ClientPacketHandlers.HandleChallengeResponse));
			PacketHandlers.Register((byte)PacketType.CHARACTER_CREATE_CITY, true, 0, new OnPacketReceive(ClientPacketHandlers.HandleCharacterCreate));
			PacketHandlers.Register((byte)PacketType.CITY_TOKEN, true, 0, new OnPacketReceive(ClientPacketHandlers.HandleCityToken));
			PacketHandlers.Register((byte)PacketType.PLAYER_SENT_LETTER, true, 0, new OnPacketReceive(ClientPacketHandlers.HandlePlayerSentLetter));
			PacketHandlers.Register((byte)PacketType.PLAYER_BROADCAST_LETTER, true, 0, new OnPacketReceive(ClientPacketHandlers.HandleBroadcastLetter));
		}
	}
}