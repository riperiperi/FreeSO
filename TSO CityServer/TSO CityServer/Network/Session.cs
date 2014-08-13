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

namespace TSO_CityServer.Network
{
    /// <summary>
    /// A session (game) in progress.
    /// </summary>
    public class Session
    {
        private Dictionary<NetworkClient, Character> m_PlayingCharacters = new Dictionary<NetworkClient, Character>();

        /// <summary>
        /// Adds a player to the current session.
        /// </summary>
        /// <param name="Client">The player's client.</param>
        /// <param name="Char">The player's character.</param>
        public void AddPlayer(NetworkClient Client, Character Char)
        {
            m_PlayingCharacters.Add(Client, Char);

            //TODO: Send state update to all players.
        }

        public void RemovePlayer(NetworkClient Client)
        {
            m_PlayingCharacters.Remove(Client);

            //TODO: Send state update to all players.
        }
    }
}
