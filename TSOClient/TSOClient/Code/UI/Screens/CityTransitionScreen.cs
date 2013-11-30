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
        private UIContainer BackgroundCtnr;
        private UIImage Background;
        private UILoginProgress LoginProgress;

        public CityTransitionScreen()
        {
            /**
             * Scale the whole screen to 1024
             */
            BackgroundCtnr = new UIContainer();
            BackgroundCtnr.ScaleX = BackgroundCtnr.ScaleY = ScreenWidth / 800.0f;

            /** Background image **/
            Background = new UIImage(GetTexture((ulong)FileIDs.UIFileIDs.setup));
            Background.ID = "Background";
            BackgroundCtnr.Add(Background);

            var lbl = new UILabel();
            lbl.Caption = "Version 1.1097.1.0";
            lbl.X = 20;
            lbl.Y = 558;
            BackgroundCtnr.Add(lbl);
            this.Add(BackgroundCtnr);

            LoginProgress = new UILoginProgress();
            LoginProgress.X = (ScreenWidth - (LoginProgress.Width + 20));
            LoginProgress.Y = (ScreenHeight - (LoginProgress.Height + 20));
            LoginProgress.Opacity = 0.9f;
            this.Add(LoginProgress);

            NetworkFacade.Controller.OnNetworkError += new NetworkErrorDelegate(Controller_OnNetworkError);
            NetworkFacade.Controller.OnCityTransitionProgress += new OnProgressDelegate(Controller_OnTransitionProgress);
            NetworkFacade.Controller.OnCityTransitionStatus += new OnCityTransitionStatusDelegate(Controller_OnCityTransitionStatus);
        }

        ~CityTransitionScreen()
        {
            NetworkFacade.Controller.OnNetworkError -= new NetworkErrorDelegate(Controller_OnNetworkError);
            NetworkFacade.Controller.OnCityTransitionProgress -= new OnProgressDelegate(Controller_OnTransitionProgress);
            NetworkFacade.Controller.OnCityTransitionStatus -= new OnCityTransitionStatusDelegate(Controller_OnCityTransitionStatus);
        }

        private void Controller_OnTransitionProgress(TSOClient.Network.Events.ProgressEvent e)
        {
            var stage = e.Done;

            LoginProgress.ProgressCaption = GameFacade.Strings.GetString("251", (stage + 4).ToString());
            LoginProgress.Progress = 25 * stage;
        }

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