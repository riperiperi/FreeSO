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
using System.Net;
using TSO_LoginServer.Network;
using GonzoNet;
using System.Configuration;
using LoginDataModel;
using ProtocolAbstractionLibraryD;

namespace TSO_LoginServer
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            /**
             * BOOTSTRAP - THIS IS WHERE THE SERVER STARTS UP
             * STEPS:
             *  > Start logging system
             *  > Load configuration
             *  > Connect to the database and test the connection
             *  > Register packet handlers
             *  > Start the login server service
             */
            Logger.Initialize("log.txt");
            //Logger.InfoEnabled = true; //Disable for release.
            Logger.DebugEnabled = true;
            Logger.WarnEnabled = true;

            GonzoNet.Logger.OnMessageLogged += new GonzoNet.MessageLoggedDelegate(Logger_OnMessageLogged);
            LoginDataModel.Logger.OnMessageLogged += new LoginDataModel.MessageLoggedDelegate(Logger_OnMessageLogged);
            ProtocolAbstractionLibraryD.Logger.OnMessageLogged += new ProtocolAbstractionLibraryD.MessageLoggedDelegate(Logger_OnMessageLogged);

            var dbConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["MAIN_DB"];
            DataAccess.ConnectionString = dbConnectionString.ConnectionString;

            /** TODO: Test the database **/
            using (var db = DataAccess.Get())
            {
                var testAccount = db.Accounts.GetByUsername("root");
                if(testAccount == null){
                    db.Accounts.Create(new Account {
                        AccountName = "root",
                        Password =  Account.GetPasswordHash("root", "root")
                    });
                }
            }

            PacketHandlers.Register((byte)PacketType.LOGIN_REQUEST, false, 0, new OnPacketReceive(LoginPacketHandlers.HandleLoginRequest));
            PacketHandlers.Register((byte)PacketType.CHARACTER_LIST, true, 0, new OnPacketReceive(LoginPacketHandlers.HandleCharacterInfoRequest));
            PacketHandlers.Register((byte)PacketType.CITY_LIST, true, 0, new OnPacketReceive(LoginPacketHandlers.HandleCityInfoRequest));
            PacketHandlers.Register((byte)PacketType.CHARACTER_CREATE, true, 0, new OnPacketReceive(LoginPacketHandlers.HandleCharacterCreate));
            PacketHandlers.Register((byte)PacketType.REQUEST_CITY_TOKEN, true, 0, new OnPacketReceive(LoginPacketHandlers.HandleCityTokenRequest));
            PacketHandlers.Register((byte)PacketType.RETIRE_CHARACTER, true, 0, new OnPacketReceive(LoginPacketHandlers.HandleCharacterRetirement));

            var Listener = new Listener();
            Listener.Initialize(Settings.BINDING);
            NetworkFacade.ClientListener = Listener;

            NetworkFacade.CServerListener = new CityServerListener();
            NetworkFacade.CServerListener.Initialize(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 2108));

            //64 is 100 in decimal.
            PacketHandlers.Register(0x64, false, 0, new OnPacketReceive(CityServerPacketHandlers.HandleCityServerLogin));
            PacketHandlers.Register(0x66, false, 3, new OnPacketReceive(CityServerPacketHandlers.HandlePulse));
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

        private void Logger_OnMessageLogged(LoginDataModel.LogMessage Msg)
        {
            switch (Msg.Level)
            {
                case LoginDataModel.LogLevel.info:
                    Logger.LogInfo(Msg.Message);
                    break;
                case LoginDataModel.LogLevel.error:
                    Logger.LogDebug(Msg.Message);
                    break;
                case LoginDataModel.LogLevel.warn:
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
    }
}
