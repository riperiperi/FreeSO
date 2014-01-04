/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the GonzoNet.

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

namespace GonzoNet
{
    public delegate void OnReceiveDelegate(PacketStream P, NetworkClient Client);

    /// <summary>
    /// Represents a listener that listens for incoming login clients.
    /// </summary>
    public class Listener
    {
        private ArrayList m_LoginClients;
        //Clients in progress of transferring to a cityserver.
        private ArrayList m_TransferringClients = new ArrayList();
        private Socket m_ListenerSock;
        private IPEndPoint m_LocalEP;

        //public event OnReceiveDelegate OnReceiveEvent;

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

        /// <summary>
        /// Initializes a new instance of Listener.
        /// </summary>
        public Listener()
        {
            m_ListenerSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_LoginClients = ArrayList.Synchronized(new ArrayList());
        }

        /// <summary>
        /// Initializes Listener. Throws SocketException if something went haywire.
        /// </summary>
        /// <param name="LocalEP">The endpoint to listen on.</param>
        public virtual void Initialize(IPEndPoint LocalEP)
        {
            m_LocalEP = LocalEP;
            
            try
            {
                m_ListenerSock.Bind(LocalEP);
                m_ListenerSock.Listen(10000);
            }
            catch (SocketException E)
            {
                throw E;
            }

            m_ListenerSock.BeginAccept(new AsyncCallback(OnAccept), m_ListenerSock);
        }

        public virtual void OnAccept(IAsyncResult AR)
        {
            Socket AcceptedSocket = m_ListenerSock.EndAccept(AR);

            if (AcceptedSocket != null)
            {
                Console.WriteLine("\nNew client connected!\r\n");

                //Let sockets linger for 5 seconds after they're closed, in an attempt to make sure all
                //pending data is sent!
                AcceptedSocket.LingerState = new LingerOption(true, 5);
                NetworkClient NewClient = new NetworkClient(AcceptedSocket, this);
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
        public void OnReceivedData(ProcessedPacket P, NetworkClient Client)
        {
            PacketHandlers.Handle(Client, P);
        }

        /// <summary>
        /// Removes a client from the internal list of connected clients.
        /// Should really only be called internally by the LoginClient.Disconnect()
        /// method.
        /// </summary>
        /// <param name="Client">The client to remove.</param>
        public void RemoveClient(NetworkClient Client)
        {
            m_LoginClients.Remove(Client);
        }

        /// <summary>
        /// Replaces a client with a modified instance.
        /// </summary>
        /// <param name="Client">The modified instance.</param>
        public virtual void UpdateClient(NetworkClient Client)
        {
            if (Client != null)
            {
                int Index = m_LoginClients.LastIndexOf(Client);
                m_LoginClients[Index] = Client;
            }
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
