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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Net.Sockets;
using System.Timers;
using System.Net;
using TSODataModel;
using TSO_CityServer.Network;
using GonzoNet;

namespace TSO_CityServer
{
    public partial class Form1 : Form
    {
        private Listener m_Listener;
        private NetworkClient m_LoginClient;
        private NetworkFacade m_NetworkFacade;

        private System.Timers.Timer m_PulseTimer;

        public Form1()
        {
            InitializeComponent();

            bool FoundConfig = ConfigurationManager.LoadCityConfig();

            Logger.Initialize("Log.txt");
            Logger.WarnEnabled = true;
            Logger.DebugEnabled = true;

            GonzoNet.Logger.OnMessageLogged += new MessageLoggedDelegate(Logger_OnMessageLogged);

            if (!FoundConfig)
            {
                Logger.LogWarning("Couldn't find a ServerConfig.ini file!");
                //TODO: This doesn't work...
                Application.Exit();
            }

            //This has to happen for the static constructor to be called...
            NetworkFacade m_NetworkFacade = new NetworkFacade();

            var dbConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["MAIN_DB"];
            DataAccess.ConnectionString = dbConnectionString.ConnectionString;

            /** TODO: Test the database **/
            using (var db = DataAccess.Get())
            {
                var testAccount = db.Accounts.GetByUsername("root");
                if (testAccount == null)
                {
                    db.Accounts.Create(new Account
                    {
                        AccountName = "root",
                        Password = "root"
                    });
                }
            }

            m_Listener = new Listener();
            //m_Listener.OnReceiveEvent += new OnReceiveDelegate(m_Listener_OnReceiveEvent);

            m_LoginClient = new NetworkClient("127.0.0.1", 2108);
            m_LoginClient.OnNetworkError += new NetworkErrorDelegate(m_LoginClient_OnNetworkError);
            m_LoginClient.OnConnected += new OnConnectedDelegate(m_LoginClient_OnConnected);
            m_LoginClient.Connect(null);

            //Send a pulse to the LoginServer every second.
            m_PulseTimer = new System.Timers.Timer(1000);
            m_PulseTimer.AutoReset = true;
            m_PulseTimer.Elapsed += new ElapsedEventHandler(m_PulseTimer_Elapsed);
            m_PulseTimer.Start();

            m_Listener.Initialize(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2107));
        }

        private void m_LoginClient_OnConnected(LoginArgsContainer LoginArgs)
        {
            LoginPacketSenders.SendServerInfo(m_LoginClient);
        }

        private void Logger_OnMessageLogged(LogMessage Msg)
        {
            switch (Msg.Level)
            {
                case LogLevel.info:
                    Logger.LogInfo(Msg.Message);
                    break;
                case LogLevel.error:
                    Logger.LogDebug(Msg.Message);
                    break;
                case LogLevel.warn:
                    Logger.LogWarning(Msg.Message);
                    break;
            }
        }

        /// <summary>
        /// Sends a pulse to the LoginServer, to let it know this server is alive.
        /// </summary>
        private void m_PulseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            TSO_CityServer.Network.PacketStream Packet = new TSO_CityServer.Network.PacketStream(0x66, 3);
            Packet.WriteByte(0x66);
            Packet.WriteUInt16(3);
            Packet.Flush();
            m_LoginClient.Send(Packet.ToArray());

            Packet.Dispose();
        }

        /// <summary>
        /// Event triggered if a network error occurs while communicating with
        /// the LoginServer.
        /// </summary>
        /// <param name="Exception"></param>
        private void m_LoginClient_OnNetworkError(SocketException Exc)
        {
            MessageBox.Show(Exc.ToString());
            Application.Exit();
        }

        /*private void m_Listener_OnReceiveEvent(PacketStream P, NetworkClient Client)
        {
            byte ID = (byte)P.ReadByte();

            switch (ID)
            {
                case 0x00:
                    PacketHandlers.HandleCharacterCreate(P, Client);
                    break;
                case 0x01:
                    PacketHandlers.HandleClientKeyReceive(P, Client);
                    break;
            }
        }*/
    }
}
