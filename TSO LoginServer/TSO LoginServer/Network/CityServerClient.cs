/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Timers;
using GonzoNet;
using ProtocolAbstractionLibraryD;

namespace TSO_LoginServer.Network
{
    public class CityServerClient : NetworkClient
    {
        //Information about this CityServer.
        //See CityServerPacketHandlers.HandleCityServerLogin().
        public CityInfo ServerInfo;

        private Timer m_PulseTimer;
        //The time when the last pulse was received from this CityServer.
        public DateTime LastPulseReceived = DateTime.Now;

        public CityServerClient(Socket ClientSocket, CityServerListener Server) : 
            base(ClientSocket, Server, GonzoNet.Encryption.EncryptionMode.AESCrypto)
        {
            m_PulseTimer = new Timer(1500);
            m_PulseTimer.AutoReset = true;
            m_PulseTimer.Elapsed += new ElapsedEventHandler(m_PulseTimer_Elapsed);
            m_PulseTimer.Start();
        }

        private void m_PulseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            double Secs = (((TimeSpan)(DateTime.Now - LastPulseReceived)).TotalMilliseconds / 1000);

            //More than 2 secs since last pulse was received, server is offline!
            if (Secs > 2.0)
            {
                Logger.LogInfo("Time since last pulse: " + Secs + " secs\r\n");
                Logger.LogInfo("More than two seconds since last pulse - disconnected CityServer.\r\n");

                this.Disconnect();
                NetworkFacade.CServerListener.CityServers.Remove(this);
                m_PulseTimer.Stop();
            }
        }
    }
}
