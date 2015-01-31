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
using CityDataModel;
using GonzoNet;
using GonzoNet.Concurrency;
using ProtocolAbstractionLibraryD;

namespace TSO_CityServer.Network
{
	/// <summary>
	/// A session (game) in progress.
	/// </summary>
	public class Session
	{
		private ConcurrentDictionary<NetworkClient, Character> m_PlayingCharacters = new ConcurrentDictionary<NetworkClient, Character>();

		public int PlayersInSession
		{
			get
			{
				return m_PlayingCharacters.Count;
			}
		}

		private ConcurrentDictionary<NetworkClient, Character> CopyPlayingCharacters()
		{
			ConcurrentDictionary<NetworkClient, Character> Copy = new ConcurrentDictionary<NetworkClient,Character>();

			lock (m_PlayingCharacters)
			{
				foreach (KeyValuePair<NetworkClient, Character> KVP in m_PlayingCharacters)
					Copy.TryAdd(KVP.Key, KVP.Value);
			}

			return Copy;
		}

		/// <summary>
		/// Adds a player to the current session.
		/// </summary>
		/// <param name="Client">The player's client.</param>
		/// <param name="Char">The player's character.</param>
		public void AddPlayer(NetworkClient Client, Character Char)
		{
			lock (m_PlayingCharacters)
			{
				foreach (KeyValuePair<NetworkClient, Character> KVP in m_PlayingCharacters)
				{
					SendPlayerJoinSession(KVP.Key, Char);
					//Send a bunch of these in reverse (to the player joining the session).
					SendPlayerJoinSession(Client, KVP.Value);

					m_PlayingCharacters.TryAdd(KVP.Key, KVP.Value);
				}
			}

			m_PlayingCharacters.TryAdd(Client, Char);
		}

		/// <summary>
		/// Removes a player from the current session.
		/// </summary>
		/// <param name="Client">The player's client.</param>
		public void RemovePlayer(NetworkClient Client)
		{
			Character Char;
			m_PlayingCharacters.TryRemove(Client, out Char);

			if (Char != null)
			{
				//This is a theoretical problem - there's no guarantee that the message will reach
				//every player in the session...
				ConcurrentDictionary<NetworkClient, Character> Copy = CopyPlayingCharacters();

				foreach (KeyValuePair<NetworkClient, Character> KVP in Copy)
					SendPlayerLeftSession(KVP.Key, KVP.Value);
			}
		}

		/// <summary>
		/// Gets all houses for all the players in a session.
		/// </summary>
		/// <returns>All houses for all the players in a session.</returns>
		public House[] GetHousesInSession()
		{
			List<House> Houses = new List<House>();

			lock(m_PlayingCharacters)
			{
				foreach(KeyValuePair<NetworkClient, Character> KVP in m_PlayingCharacters)
				{
					if (KVP.Value.HouseHouse != null)
						Houses.Add(KVP.Value.HouseHouse);
				}
			}

			return Houses.ToArray();
		}

		/// <summary>
		/// Gets a player's character from the session.
		/// </summary>
		/// <param name="GUID">The GUID of the character to retrieve.</param>
		/// <returns>A Character instance, null if not found.</returns>
		public Character GetPlayer(string GUID)
		{
			lock (m_PlayingCharacters)
			{
				foreach (KeyValuePair<NetworkClient, Character> KVP in m_PlayingCharacters)
				{
					if (KVP.Value.GUID.ToString().Equals(GUID, StringComparison.CurrentCultureIgnoreCase))
						return KVP.Value;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets a player's character from the session.
		/// </summary>
		/// <param name="Client">The NetworkClient instance of the player to retrieve.</param>
		/// <returns>A Character instance, null if not found.</returns>
		public Character GetPlayer(NetworkClient Client)
		{
			if (m_PlayingCharacters.ContainsKey(Client))
				return m_PlayingCharacters[Client];
			else
				return null;
		}

		/// <summary>
		/// Gets a player's client from the session.
		/// </summary>
		/// <param name="GUID">The GUID of the character.</param>
		/// <returns>A NetworkClient instance, null if not found.</returns>
		public NetworkClient GetPlayersClient(string GUID)
		{
			lock (m_PlayingCharacters)
			{
				foreach (KeyValuePair<NetworkClient, Character> KVP in m_PlayingCharacters)
				{
					if (KVP.Value.GUID.ToString().Equals(GUID, StringComparison.CurrentCultureIgnoreCase))
					{
						m_PlayingCharacters.TryAdd(KVP.Key, KVP.Value);
						return KVP.Key;
					}
				}
			}

			return null;
		}

		#region Sending

		/// <summary>
		/// A new player joined the current session!
		/// </summary>
		/// <param name="Client">Client to inform about new player.</param>
		public void SendPlayerJoinSession(NetworkClient Client, Character Player)
		{
			PacketStream JoinPacket = new PacketStream((byte)PacketType.PLAYER_JOINED_SESSION, 0);
			JoinPacket.WriteString(Player.GUID.ToString());
			JoinPacket.WriteString(Player.Name);
			JoinPacket.WriteString(Player.Sex);
			JoinPacket.WriteString(Player.Description);
			JoinPacket.WriteInt64(Player.HeadOutfitID);
			JoinPacket.WriteInt64(Player.BodyOutfitID);
			JoinPacket.WriteInt32(Player.AppearanceType);

			Client.SendEncrypted((byte)PacketType.PLAYER_JOINED_SESSION, JoinPacket.ToArray());
		}

		/// <summary>
		/// A new player left the current session!
		/// </summary>
		/// <param name="Client">Client to inform about player leaving.</param>
		public void SendPlayerLeftSession(NetworkClient Client, Character Player)
		{
			PacketStream JoinPacket = new PacketStream((byte)PacketType.PLAYER_LEFT_SESSION, 0);
			JoinPacket.WriteString(Player.GUID.ToString());

			Client.SendEncrypted((byte)PacketType.PLAYER_LEFT_SESSION, JoinPacket.ToArray());
		}

		/// <summary>
		/// Player received a letter from another player.
		/// </summary>
		/// <param name="Client">Client of receiving player.</param>
		/// <param name="Subject">Letter's subject.</param>
		/// <param name="Msg">Letter's body.</param>
		/// <param name="LetterFrom">Name of player sending the letter.</param>
		public void SendPlayerReceivedLetter(NetworkClient Client, string Subject, string Msg, string LetterFrom)
		{
			PacketStream Packet = new PacketStream((byte)PacketType.PLAYER_RECV_LETTER, 0);
			Packet.WriteString(LetterFrom);
			Packet.WriteString(Subject);
			Packet.WriteString(Msg);

			Client.SendEncrypted((byte)PacketType.PLAYER_RECV_LETTER, Packet.ToArray());
		}

		/// <summary>
		/// A letter was broadcast to all players.
		/// </summary>
		/// <param name="Client">Client of player sending the letter.</param>
		/// <param name="Subject">Letter's subject.</param>
		/// <param name="Msg">Letter's body.</param>
		public void SendBroadcastLetter(NetworkClient Client, string Subject, string Msg)
		{
			PacketStream Packet = new PacketStream((byte)PacketType.PLAYER_RECV_LETTER, 0);
			Packet.WriteString(Subject);
			Packet.WriteString(Msg);

			//This is a theoretical problem - there's no guarantee that the message will reach
			//every player in the session...
			ConcurrentDictionary<NetworkClient, Character> Copy = CopyPlayingCharacters();

			foreach (KeyValuePair<NetworkClient, Character> KVP in Copy)
			{
				if (Client != KVP.Key)
					KVP.Key.SendEncrypted((byte)PacketType.PLAYER_RECV_LETTER, Packet.ToArray());
			}
		}

		/// <summary>
		/// Time of day in neighbourhood.
		/// </summary>
		/// <param name="TimeOfDay">The time of day, represented as scale from 0 to 1.</param>
		public void SendTimeOfDay(int Hours, int Minutes, int Seconds)
		{
			PacketStream Packet = new PacketStream((byte)PacketType.TIME_OF_DAY, 0);
			Packet.WriteInt32(Hours);
			Packet.WriteInt32(Minutes);
			Packet.WriteInt32(Seconds);

			//Is this a problem? Probably not, if a player misses one ToD message, it'll
			//just get the next one.
			ConcurrentDictionary<NetworkClient, Character> Copy = CopyPlayingCharacters();

			foreach (KeyValuePair<NetworkClient, Character> KVP in Copy)
					KVP.Key.SendEncrypted((byte)PacketType.TIME_OF_DAY, Packet.ToArray());
		}

		#endregion
	}
}