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
    public class LoginScreen : GameScreen
    {
        private UIContainer BackgroundCtnr;
        private UIImage Background;
        private UILoginDialog LoginDialog;
        private UILoginProgress LoginProgress;

        public LoginScreen()
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

            LoginDialog = new UILoginDialog(this);
            LoginDialog.Opacity = 0.9f;
            //Center
            LoginDialog.X = (ScreenWidth - LoginDialog.Width) / 2;
            LoginDialog.Y = (ScreenHeight - LoginDialog.Height) / 2;
            this.Add(LoginDialog);

            NetworkFacade.Controller.OnNetworkError += new NetworkErrorDelegate(Controller_OnNetworkError);
            NetworkFacade.Controller.OnLoginProgress += new OnProgressDelegate(Controller_OnLoginProgress);
            NetworkFacade.Controller.OnLoginStatus += new OnLoginStatusDelegate(Controller_OnLoginStatus);
        }

        ~LoginScreen()
        {
            NetworkFacade.Controller.OnNetworkError -= new NetworkErrorDelegate(Controller_OnNetworkError);
            NetworkFacade.Controller.OnLoginProgress -= new OnProgressDelegate(Controller_OnLoginProgress);
            NetworkFacade.Controller.OnLoginStatus -= new OnLoginStatusDelegate(Controller_OnLoginStatus);
        }

        void Controller_OnLoginProgress(TSOClient.Network.Events.ProgressEvent e)
        {
            var stage = e.Done;

            LoginProgress.ProgressCaption = GameFacade.Strings.GetString("210", (stage + 4).ToString());
            LoginProgress.Progress = 25 * stage;
        }

        void Controller_OnLoginStatus(TSOClient.Network.Events.LoginEvent e)
        {
            m_InLogin = false;
            if (e.Success)
            {
                /** Go to the select a sim page, make sure we do this in the UIThread **/
                GameFacade.Controller.ShowPersonSelection();
            }
            else
            {
                if (e.VersionOK)
                {
                    UIAlertOptions Options = new UIAlertOptions();
                    Options.Message = GameFacade.Strings.GetString("210", "26 110");
                    Options.Title = GameFacade.Strings.GetString("210", "21");
                    Options.Buttons = UIAlertButtons.OK;
                    UI.Framework.UIScreen.ShowAlert(Options, true);

                    /** Reset **/
                    LoginProgress.ProgressCaption = GameFacade.Strings.GetString("210", "4");
                    LoginProgress.Progress = 0;
                    m_InLogin = false;
                }
                else
                {
                    UIAlertOptions Options = new UIAlertOptions();
                    Options.Message = "Your client was not up to date!";
                    Options.Title = "Invalid version";
                    Options.Buttons = UIAlertButtons.OK;
                    UI.Framework.UIScreen.ShowAlert(Options, true);

                    /** Reset **/
                    LoginProgress.ProgressCaption = GameFacade.Strings.GetString("210", "4");
                    LoginProgress.Progress = 0;
                    m_InLogin = false;
                }
            }
        }


        private bool m_InLogin = false;
        /// <summary>
        /// Called by login button click in UILoginDialog
        /// </summary>
        public void Login(){
            if (m_InLogin) { return; }
            m_InLogin = true;

            Controller_OnLoginProgress(new TSOClient.Network.Events.ProgressEvent(TSOClient.Events.EventCodes.PROGRESS_UPDATE) { Done = 1 });
            NetworkFacade.Controller.InitialConnect(LoginDialog.Username.ToUpper(), LoginDialog.Password.ToUpper());
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
            LoginProgress.ProgressCaption = GameFacade.Strings.GetString("210", "4");
            LoginProgress.Progress = 0;
            m_InLogin = false;
        }
    }
}
