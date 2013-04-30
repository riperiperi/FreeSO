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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using TSO_LoginServer.Network;

namespace TSO_LoginServer
{
    public partial class Form1 : Form
    {
        private LoginListener m_Listener;
        private CityServerListener m_CServerListener;

        public Form1()
        {
            InitializeComponent();

            try
            {
                //MySQLDatabase.Connect();
                Database.Connect();
            }
            catch (NoDBConnection NoDB)
            {
                MessageBox.Show(NoDB.Message);
                Environment.Exit(0);
            }

            Logger.Initialize("Log.txt");
            Logger.InfoEnabled = true;
            Logger.WarnEnabled = true;

            m_Listener = new LoginListener();
            m_Listener.OnReceiveEvent += new OnReceiveDelegate(m_Listener_OnReceiveEvent);

            //LoginRequest - Variable size.
            LoginClient.RegisterLoginPacketID(0x00, 0);
            //CharacterInfoRequest - Variable size.
            LoginClient.RegisterLoginPacketID(0x05, 0);
            //CharacterCreate - Variable size.
            LoginClient.RegisterLoginPacketID(0x06, 0);

            m_Listener.Initialize(2106);

            m_CServerListener = new CityServerListener();
            m_CServerListener.OnReceiveEvent += new OnCityReceiveDelegate(m_CServerListener_OnReceiveEvent);

            //CityServerLogin - Variable size.
            CityServerClient.RegisterPatchPacketID(0x00, 0);

            m_CServerListener.Initialize(2348);
        }

        /// <summary>
        /// Handles incoming packets from a CityServer.
        /// </summary>
        void m_CServerListener_OnReceiveEvent(PacketStream P, CityServerClient Client)
        {
                byte ID = (byte)P.ReadByte();

                switch (ID)
                {
                    case 0x00:
                        CityServerPacketHandlers.HandleCityServerLogin(P, ref Client);
                        break;
                    case 0x01:
                        CityServerPacketHandlers.HandleKeyFetch(ref m_Listener, P, Client);
                        break;
                }
        }

        /// <summary>
        /// Handles incoming packets from connected clients.
        /// </summary>
        void m_Listener_OnReceiveEvent(PacketStream P, LoginClient Client)
        {
            byte ID = (byte)P.ReadByte();

            switch (ID)
            {
                case 0x00:
                    PacketHandlers.HandleLoginRequest(P, ref Client);
                    break;
                case 0x05:
                    PacketHandlers.HandleCharacterInfoRequest(P, Client);
                    break;
                case 0x06:
                    PacketHandlers.HandleCharacterCreate(P, ref Client, ref m_CServerListener);
                    break;
                default:
                    Logger.LogInfo("Received unhandled packet - ID: " + P.PacketID);
                    break;
            }
        }
    }
}
