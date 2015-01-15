/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
using System.Security.Cryptography;
using GonzoNet.Encryption;
using GonzoNet.Concurrency;

namespace GonzoNet
{
    public delegate void OnReceiveDelegate(PacketStream P, NetworkClient Client);
    public delegate void OnDisconnectedDelegate(NetworkClient Client);

    /// <summary>
    /// Represents a listener that listens for incoming login clients.
    /// </summary>
    public class Listener
    {
        private BlockingCollection<NetworkClient> m_LoginClients;
        private Socket m_ListenerSock;
        private IPEndPoint m_LocalEP;

        private EncryptionMode m_EMode;

        public event OnDisconnectedDelegate OnDisconnected;

        public BlockingCollection<NetworkClient> Clients
        {
            get { return m_LoginClients; }
        }

        /// <summary>
        /// Initializes a new instance of Listener.
        /// </summary>
        public Listener(EncryptionMode Mode)
        {
            m_ListenerSock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            m_LoginClients = new BlockingCollection<NetworkClient>();

            m_EMode = Mode;
            /*switch (Mode)
            {
                case EncryptionMode.AESCrypto:
                    m_AesCryptoService = new AesCryptoServiceProvider();
                    m_AesCryptoService.GenerateIV();
                    m_AesCryptoService.GenerateKey();
                    break;
            }*/
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

        /// <summary>
        /// Callback for accepting connections.
        /// </summary>
        public virtual void OnAccept(IAsyncResult AR)
        {
            Socket AcceptedSocket = m_ListenerSock.EndAccept(AR);

            if (AcceptedSocket != null)
            {
                Console.WriteLine("\nNew client connected!\r\n");

                //Let sockets linger for 5 seconds after they're closed, in an attempt to make sure all
                //pending data is sent!
                AcceptedSocket.LingerState = new LingerOption(true, 5);
                NetworkClient NewClient = new NetworkClient(AcceptedSocket, this, m_EMode);

                switch (m_EMode)
                {
                    case EncryptionMode.AESCrypto:
                        NewClient.ClientEncryptor = new AESEncryptor("");
                        break;
                }

                m_LoginClients.Add(NewClient);
            }

            m_ListenerSock.BeginAccept(new AsyncCallback(OnAccept), m_ListenerSock);
        }

        /// <summary>
        /// Removes a client from the internal list of connected clients.
        /// Should really only be called internally by the NetworkClient.Disconnect()
        /// method.
        /// </summary>
        /// <param name="Client">The client to remove.</param>
        public virtual void RemoveClient(NetworkClient Client)
        {
            m_LoginClients.TryRemove(out Client);
            //TODO: Store session data for client...

            if (OnDisconnected != null)
                OnDisconnected(Client);
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