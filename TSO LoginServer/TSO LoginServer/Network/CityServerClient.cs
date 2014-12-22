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

            //More than 3,5 secs since last pulse was received, server is offline!
            if (Secs > 3.5)
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
