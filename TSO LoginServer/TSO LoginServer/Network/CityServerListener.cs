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
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using GonzoNet;
using GonzoNet.Encryption;
using ProtocolAbstractionLibraryD;

namespace TSO_LoginServer.Network
{
    public class CityServerListener : Listener
    {
        public BlockingCollection<CityInfo> CityServers;
        private Socket m_ListenerSock;
        private IPEndPoint m_LocalEP;

        public CityServerListener(EncryptionMode EncMode) : base(EncMode)
        {
            m_ListenerSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            CityServers = new BlockingCollection<CityInfo>();
        }

        public override void Initialize(IPEndPoint LocalEP)
        {
            m_LocalEP = LocalEP;

            try
            {
                m_ListenerSock.Bind(LocalEP);
                m_ListenerSock.Listen(10000);

                Logger.LogDebug("Started listening on: " + LocalEP.Address.ToString()
                    + ":" + LocalEP.Port + "\r\n");
            }
            catch (SocketException E)
            {
                Logger.LogWarning("Winsock error caused by call to Socket.Bind(): \n" + E.ToString() + "\r\n");
            }

            m_ListenerSock.BeginAccept(new AsyncCallback(OnAccept), m_ListenerSock);
        }

        /// <summary>
        /// One of the city servers sent a pulse.
        /// </summary>
        /// <param name="Client">The city server's client.</param>
        public void OnReceivedPulse(NetworkClient Client)
        {
            foreach(CityInfo Info in CityServers.GetConsumingEnumerable())
            {
                if(Client == Info.Client)
                {
                    Info.Online = true;
                    Info.LastPulseReceived = DateTime.Now;
                    CityServers.Add(Info);

					break;
                }

				CityServers.Add(Info);
            }
        }

        public override void OnAccept(IAsyncResult AR)
        {
            Socket AcceptedSocket = m_ListenerSock.EndAccept(AR);

            if (AcceptedSocket != null)
            {
                Logger.LogDebug("\nNew cityserver connected!\r\n");

                //Let sockets linger for 5 seconds after they're closed, in an attempt to make sure all
                //pending data is sent!
                AcceptedSocket.LingerState = new LingerOption(true, 5);

                CityInfo Info = new CityInfo(true);
                Info.Client = new NetworkClient(AcceptedSocket, this, EncryptionMode.NoEncryption);

                CityServers.Add(Info);
            }

            m_ListenerSock.BeginAccept(new AsyncCallback(OnAccept), m_ListenerSock);
        }
    }
}
