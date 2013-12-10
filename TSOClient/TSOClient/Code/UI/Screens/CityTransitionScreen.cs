/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.UI.Panels;
using TSOClient.Network;
using GonzoNet;

namespace TSOClient.Code.UI.Screens
{
    public class CityTransitionScreen : GameScreen
    {
        private UIContainer m_BackgroundCtnr;
        private UIImage m_Background;
        private UILoginProgress m_LoginProgress;
        private CityInfo m_SelectedCity;

        public CityTransitionScreen(CityInfo SelectedCity)
        {
            m_SelectedCity = SelectedCity;

            /**
             * Scale the whole screen to 1024
             */
            m_BackgroundCtnr = new UIContainer();
            m_BackgroundCtnr.ScaleX = m_BackgroundCtnr.ScaleY = ScreenWidth / 800.0f;

            /** Background image **/
            m_Background = new UIImage(GetTexture((ulong)FileIDs.UIFileIDs.setup));
            m_Background.ID = "Background";
            m_BackgroundCtnr.Add(m_Background);

            var lbl = new UILabel();
            lbl.Caption = "Version 1.1097.1.0";
            lbl.X = 20;
            lbl.Y = 558;
            m_BackgroundCtnr.Add(lbl);
            this.Add(m_BackgroundCtnr);

            m_LoginProgress = new UILoginProgress();
            m_LoginProgress.X = (ScreenWidth - (m_LoginProgress.Width + 20));
            m_LoginProgress.Y = (ScreenHeight - (m_LoginProgress.Height + 20));
            m_LoginProgress.Opacity = 0.9f;
            this.Add(m_LoginProgress);

            NetworkFacade.Controller.OnNetworkError += new NetworkErrorDelegate(Controller_OnNetworkError);
            NetworkFacade.Controller.OnCityTransitionProgress += new OnProgressDelegate(Controller_OnTransitionProgress);
            NetworkFacade.Controller.OnCityTransitionStatus += new OnCityTransitionStatusDelegate(Controller_OnCityTransitionStatus);

            LoginArgsContainer LoginArgs = new LoginArgsContainer();
            LoginArgs.Username = NetworkFacade.Client.ClientEncryptor.Username;
            LoginArgs.Enc = NetworkFacade.Client.ClientEncryptor;

            NetworkFacade.Client = new NetworkClient(SelectedCity.IP, SelectedCity.Port);
            //THIS IS IMPORTANT - THIS NEEDS TO BE COPIED AFTER IT HAS BEEN RECREATED FOR
            //THE RECONNECTION TO WORK!
            LoginArgs.Client = NetworkFacade.Client;
            NetworkFacade.Client.OnConnected += new OnConnectedDelegate(Client_OnConnected);
            NetworkFacade.Controller.Reconnect(ref NetworkFacade.Client, SelectedCity, LoginArgs);
        }

        ~CityTransitionScreen()
        {
            NetworkFacade.Controller.OnNetworkError -= new NetworkErrorDelegate(Controller_OnNetworkError);
            NetworkFacade.Controller.OnCityTransitionProgress -= new OnProgressDelegate(Controller_OnTransitionProgress);
            NetworkFacade.Controller.OnCityTransitionStatus -= new OnCityTransitionStatusDelegate(Controller_OnCityTransitionStatus);
        }

        private void Client_OnConnected(LoginArgsContainer LoginArgs)
        {
            TSOClient.Network.Events.ProgressEvent Progress = 
                new TSOClient.Network.Events.ProgressEvent(TSOClient.Events.EventCodes.PROGRESS_UPDATE);
            Progress.Done = 1;
            Progress.Total = 2;

            UIPacketSenders.SendCharacterCreateCity(LoginArgs, PlayerAccount.CurrentlyActiveSim);
            Controller_OnTransitionProgress(Progress);
        }

        /// <summary>
        /// Another stage in the CityServer transition progress was done.
        /// </summary>
        /// <param name="e"></param>
        private void Controller_OnTransitionProgress(TSOClient.Network.Events.ProgressEvent e)
        {
            var stage = e.Done;

            m_LoginProgress.ProgressCaption = GameFacade.Strings.GetString("251", (stage + 4).ToString());
            m_LoginProgress.Progress = 25 * stage;
        }

        /// <summary>
        /// Sucessfully transitioned to CityServer, so show the city.
        /// </summary>
        private void Controller_OnCityTransitionStatus(TSOClient.Network.Events.CityTransitionEvent e)
        {
            if (e.Success)
                GameFacade.Controller.ShowCity();
        }

        /// <summary>
        /// A network error occured - 95% of the time, this will be because
        /// a connection could not be established.
        /// </summary>
        /// <param name="Exception">The exception that occured.</param>
        private void Controller_OnNetworkError(SocketException Exception)
        {
            UIAlertOptions Options = new UIAlertOptions();
            Options.Message = GameFacade.Strings.GetString("210", "36 301");
            Options.Title = GameFacade.Strings.GetString("210", "40");
            Options.Buttons = UIAlertButtons.OK;
            UI.Framework.UIScreen.ShowAlert(Options, true);

            /** Reset **/
            //Note: A network error *should* never occur in this screen, so this code should never be called.
            GameFacade.Controller.ShowPersonSelection();
        }
    }
}