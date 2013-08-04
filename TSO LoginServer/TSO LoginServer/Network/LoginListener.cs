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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace TSO_LoginServer.Network
{
    public delegate void OnReceiveDelegate(PacketStream P, LoginClient Client);

    /// <summary>
    /// Represents a listener that listens for incoming login clients.
    /// </summary>
    public class LoginListener //: Listener
    {
        private ArrayList m_LoginClients;
        //Clients in progress of transferring to a cityserver.
        private ArrayList m_TransferringClients = new ArrayList();
        private Socket m_ListenerSock;
        private IPEndPoint m_LocalEP;

        public event OnReceiveDelegate OnReceiveEvent;

        public ArrayList Clients
        {
            get { return m_LoginClients; }
        }

        /// <summary>
        /// Clients that are transferring to a CityServer.
        /// </summary>
        public ArrayList TransferringClients
        {
            get { return m_TransferringClients; }
        }

        public LoginListener()
        {
            m_ListenerSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_LoginClients = ArrayList.Synchronized(new ArrayList());
        }

        public void Initialize(int Port)
        {
            IPEndPoint LocalEP;

            switch (GlobalSettings.Default.ListeningIP)
            {
                case "IPAddress.Any":
                    LocalEP = new IPEndPoint(IPAddress.Any, Port);
                    break;
                default:
                    LocalEP = new IPEndPoint(IPAddress.Parse(GlobalSettings.Default.ListeningIP), Port);
                    break;
            }

            m_LocalEP = LocalEP;

            try
            {
                m_ListenerSock.Bind(LocalEP);
                m_ListenerSock.Listen(10000);

                Logger.LogInfo("Started listening on: " + LocalEP.Address.ToString()
                    + ":" + LocalEP.Port + "\r\n");
            }
            catch (SocketException E)
            {
                Logger.LogWarning("Winsock error caused by call to Socket.Bind(): \n" + E.ToString() + "\r\n");
            }

            m_ListenerSock.BeginAccept(new AsyncCallback(OnAccept), m_ListenerSock);
        }

        public void OnAccept(IAsyncResult AR)
        {
            Socket AcceptedSocket = m_ListenerSock.EndAccept(AR);

            if (AcceptedSocket != null)
            {
                Console.WriteLine("\nNew client connected!\r\n");

                //Let sockets linger for 5 seconds after they're closed, in an attempt to make sure all
                //pending data is sent!
                AcceptedSocket.LingerState = new LingerOption(true, 5);
                LoginClient NewClient = new LoginClient(AcceptedSocket, this);
                m_LoginClients.Add(NewClient);
            }

            m_ListenerSock.BeginAccept(new AsyncCallback(OnAccept), m_ListenerSock);
        }

        /// <summary>
        /// Called by LoginClient instances
        /// when they've received some new data
        /// (a new packet). Should not be called
        /// from anywhere else.
        /// </summary>
        /// <param name="P"></param>
        public void OnReceivedData(PacketStream P, LoginClient Client)
        {
            OnReceiveEvent(P, Client);
        }

        /// <summary>
        /// Removes a client from the internal list of connected clients.
        /// Should really only be called internally by the LoginClient.Disconnect()
        /// method.
        /// </summary>
        /// <param name="Client">The client to remove.</param>
        public void RemoveClient(LoginClient Client)
        {
            m_LoginClients.Remove(Client);
        }

        /// <summary>
        /// Moves a client from the list of clients that are connected to this
        /// LoginServer to the list of clients that are transferring to a CityServer.
        /// </summary>
        /// <param name="Client">The client to move.</param>
        public void TransferClient(LoginClient Client)
        {
            m_LoginClients.Remove(Client);
            m_TransferringClients.Add(Client);
        }

        /// <summary>
        /// The number of clients that are connected to this LoginListener instance.
        /// </summary>
        public int NumConnectedClients
        {
            get { return m_LoginClients.Count; }
        }
    }
}
