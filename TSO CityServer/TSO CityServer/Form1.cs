/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
using System.Diagnostics;
using System.Threading;
using CityDataModel;
using TSO_CityServer.Network;
using GonzoNet;
using GonzoNet.Encryption;

namespace TSO_CityServer
{
    public partial class Form1 : Form
    {
        private NetworkClient m_LoginClient;

        private System.Timers.Timer m_PulseTimer;

        public Form1()
        {
            InitializeComponent();

            bool FoundConfig = ConfigurationManager.LoadCityConfig();

            Logger.Initialize("Log.txt");
            Logger.WarnEnabled = true;
            Logger.DebugEnabled = true;

            GonzoNet.Logger.OnMessageLogged += new GonzoNet.MessageLoggedDelegate(Logger_OnMessageLogged);
            CityDataModel.Logger.OnMessageLogged += new CityDataModel.MessageLoggedDelegate(Logger_OnMessageLogged);
            ProtocolAbstractionLibraryD.Logger.OnMessageLogged += new ProtocolAbstractionLibraryD.MessageLoggedDelegate(Logger_OnMessageLogged);

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

            NetworkFacade.NetworkListener = new Listener(EncryptionMode.AESCrypto);
            //Remove a player from the current session when it disconnects.
            NetworkFacade.NetworkListener.OnDisconnected += new OnDisconnectedDelegate(NetworkFacade.CurrentSession.RemovePlayer);

            m_LoginClient = new NetworkClient("127.0.0.1", 2108, EncryptionMode.AESCrypto);
            m_LoginClient.OnNetworkError += new NetworkErrorDelegate(m_LoginClient_OnNetworkError);
            m_LoginClient.OnConnected += new OnConnectedDelegate(m_LoginClient_OnConnected);
            m_LoginClient.Connect(null);

            //Send a pulse to the LoginServer every second.
            m_PulseTimer = new System.Timers.Timer(1000);
            m_PulseTimer.AutoReset = true;
            m_PulseTimer.Elapsed += new ElapsedEventHandler(m_PulseTimer_Elapsed);
            m_PulseTimer.Start();

            NetworkFacade.NetworkListener.Initialize(Settings.BINDING);
        }

        private void m_LoginClient_OnConnected(LoginArgsContainer LoginArgs)
        {
            LoginPacketSenders.SendServerInfo(m_LoginClient);
        }

        #region Log Sink

        void Logger_OnMessageLogged(ProtocolAbstractionLibraryD.LogMessage Msg)
        {
            switch (Msg.Level)
            {
                case ProtocolAbstractionLibraryD.LogLevel.info:
                    Logger.LogInfo(Msg.Message);
                    break;
                case ProtocolAbstractionLibraryD.LogLevel.error:
                    Logger.LogDebug(Msg.Message);
                    break;
                case ProtocolAbstractionLibraryD.LogLevel.warn:
                    Logger.LogWarning(Msg.Message);
                    break;
            }
        }

        private void Logger_OnMessageLogged(CityDataModel.LogMessage Msg)
        {
            switch (Msg.Level)
            {
                case CityDataModel.LogLevel.info:
                    Logger.LogInfo(Msg.Message);
                    break;
                case CityDataModel.LogLevel.error:
                    Logger.LogDebug(Msg.Message);
                    break;
                case CityDataModel.LogLevel.warn:
                    Logger.LogWarning(Msg.Message);
                    break;
            }
        }

        private void Logger_OnMessageLogged(GonzoNet.LogMessage Msg)
        {
            switch (Msg.Level)
            {
                case GonzoNet.LogLevel.info:
                    Logger.LogInfo(Msg.Message);
                    break;
                case GonzoNet.LogLevel.error:
                    Logger.LogDebug(Msg.Message);
                    break;
                case GonzoNet.LogLevel.warn:
                    Logger.LogWarning(Msg.Message);
                    break;
            }
        }

        #endregion

        /// <summary>
        /// Sends a pulse to the LoginServer, to let it know this server is alive.
        /// </summary>
        private void m_PulseTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            PacketStream Packet = new PacketStream(0x66, 3);
            Packet.WriteByte(0x66);
            Packet.WriteUInt16(3);
            Packet.Flush();

            lock(m_LoginClient)
                m_LoginClient.Send(Packet.ToArray());

            Debug.WriteLine("Sent pulse!");

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
    }
}
