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
using System.Configuration;
using TSODataModel;

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
            Logger.InfoEnabled = true;
            Logger.DebugEnabled = true;
            Logger.WarnEnabled = true;

            var dbConnectionString = ConfigurationManager.ConnectionStrings["MAIN_DB"];
            DataAccess.ConnectionString = dbConnectionString.ConnectionString;

            /** TODO: Test the database **/
            using (var db = DataAccess.Get())
            {
                var testAccount = db.Accounts.GetByUsername("root");
                if(testAccount == null){
                    db.Accounts.Create(new Account {
                        AccountName = "root",
                        Password =  "root"/*Account.GetPasswordHash("password", "username")*/
                    });
                }
            }

            PacketHandlers.Init();


            var Listener = new LoginListener();
            Listener.Initialize(Settings.BINDING);
            NetworkFacade.ClientListener = Listener;

            NetworkFacade.CServerListener = new CityServerListener();
            NetworkFacade.CServerListener.Initialize(2108);
            NetworkFacade.CServerListener.OnReceiveEvent += new OnCityReceiveDelegate(m_CServerListener_OnReceiveEvent);

            ////CityServerLogin - Variable size.
            CityServerClient.RegisterCityPacketID(0x00, 0);
            ////KeyFetch - Variable size.
            CityServerClient.RegisterCityPacketID(0x01, 0);
            ////Pulse - two bytes.
            CityServerClient.RegisterCityPacketID(0x02, 3);

            //NetworkFacade.CServerListener.Initialize(2348);
        }

        /// <summary>
        /// Handles incoming packets from a CityServer.
        /// </summary>
        private void m_CServerListener_OnReceiveEvent(PacketStream P, ref CityServerClient Client)
        {
                byte ID = (byte)P.ReadByte();

                switch (ID)
                {
                    case 0x00:
                        CityServerPacketHandlers.HandleCityServerLogin(P, ref Client);
                        break;
                    case 0x01:
                        CityServerPacketHandlers.HandleKeyFetch(ref NetworkFacade.ClientListener, P, Client);
                        break;
                    case 0x02:
                        CityServerPacketHandlers.HandlePulse(P, ref Client);
                        break;
                }
        }
    }
}
