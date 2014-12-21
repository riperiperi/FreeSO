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

namespace TSO_CityServer.Network
{
    /// <summary>
    /// A session (game) in progress.
    /// </summary>
    public class Session
    {
        private ConcurrentDictionary<NetworkClient, Character> m_PlayingCharacters = new ConcurrentDictionary<NetworkClient, Character>();

        /// <summary>
        /// Adds a player to the current session.
        /// </summary>
        /// <param name="Client">The player's client.</param>
        /// <param name="Char">The player's character.</param>
        public void AddPlayer(NetworkClient Client, Character Char)
        {
            foreach (KeyValuePair<NetworkClient, Character> KVP in m_PlayingCharacters)
            {
                ClientPacketSenders.SendPlayerJoinSession(KVP.Key, Char);
                //Send a bunch of these in reverse (to the player joining the session).
                ClientPacketSenders.SendPlayerJoinSession(Client, KVP.Value);

				m_PlayingCharacters.TryAdd(KVP.Key, KVP.Value);
            }

            m_PlayingCharacters.TryAdd(Client, Char);
        }

        /// <summary>
        /// Removes a player from the current session.
        /// </summary>
        /// <param name="Client">The player's client.</param>
        public void RemovePlayer(NetworkClient Client)
        {
            Character Char = m_PlayingCharacters[Client]; //NOTE: This might already be removing it...
			m_PlayingCharacters.TryRemove(Client, out Char);

			foreach (KeyValuePair<NetworkClient, Character> KVP in m_PlayingCharacters)
			{
				ClientPacketSenders.SendPlayerLeftSession(KVP.Key, m_PlayingCharacters[Client]);
				m_PlayingCharacters.TryAdd(KVP.Key, KVP.Value);
			}
        }

        /// <summary>
        /// Gets a player's character from the session.
        /// </summary>
        /// <param name="GUID">The GUID of the character to retrieve.</param>
        /// <returns>A Character instance, null if not found.</returns>
        public Character GetPlayer(string GUID)
        {
            foreach (KeyValuePair<NetworkClient, Character> KVP in m_PlayingCharacters)
            {
                if (KVP.Value.GUID.ToString().Equals(GUID, StringComparison.CurrentCultureIgnoreCase))
                    return KVP.Value;
            }

            return null;
        }

        /// <summary>
        /// Gets a player's client from the session.
        /// </summary>
        /// <param name="GUID">The GUID of the character.</param>
        /// <returns>A NetworkClient instance, null if not found.</returns>
        public NetworkClient GetPlayersClient(string GUID)
        {
            foreach (KeyValuePair<NetworkClient, Character> KVP in m_PlayingCharacters)
            {
				if (KVP.Value.GUID.ToString().Equals(GUID, StringComparison.CurrentCultureIgnoreCase))
				{
					m_PlayingCharacters.TryAdd(KVP.Key, KVP.Value);
					return KVP.Key;
				}
            }

            return null;
        }
    }
}
