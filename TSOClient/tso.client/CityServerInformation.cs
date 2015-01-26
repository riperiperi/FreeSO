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
using System.Linq;
using System.Text;

namespace TSOClient
{
    /// <summary>
    /// Contains information about a cityserver (gameserver).
    /// Sent from the loginserver when a client has created a new Sim
    /// so that the client can choose which city to move the Sim into.
    /// </summary>
    class CityServerInformation
    {
        private string m_CityName, m_CityDescription;
        private ulong m_ImageID;
        private string m_IP;
        private int m_Port;

        public string CityName
        {
            get { return m_CityName; }
        }

        public string CityDescription
        {
            get { return m_CityDescription; }
        }

        public string IP
        {
            get { return m_IP; }
        }

        public int Port
        {
            get { return m_Port; }
        }

        public ulong ImageID
        {
            get { return m_ImageID; }
        }

        public CityServerInformation(string CityName, string CityDescription, ulong ImageID, string IP, int Port)
        {
            m_CityName = CityName;
            m_CityDescription = CityDescription;
            m_ImageID = ImageID;
            m_IP = IP;
            m_Port = Port;
        }
    }
}
