/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

The Original Code is the TSO LoginServer.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.IO;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.UI.Panels;
using TSOClient.Network;
using TSOClient.LUI;

using GonzoNet;
using TSO.Files.XA;
using TSO.Content;
using Un4seen.Bass;
using System.Runtime.InteropServices;

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
            PlayBackgroundMusic(new string[] { "none" });

            TSO.HIT.HITVM.Get().PlaySoundEvent("lot_enter");

            /**
             * Scale the whole screen to 1024
             */
            BackgroundCtnr = new UIContainer();
            BackgroundCtnr.ScaleX = BackgroundCtnr.ScaleY = ScreenWidth / 800.0f;

            /** Background image **/
            Background = new UIImage(GetTexture((ulong)FileIDs.UIFileIDs.setup));
            Background.ID = "Background";
            BackgroundCtnr.Add(Background);

            /** Client version **/
            var lbl = new UILabel();
            lbl.Caption = "Version " + GlobalSettings.Default.ClientVersion;
            lbl.X = 20;
            lbl.Y = 558;
            BackgroundCtnr.Add(lbl);
            this.Add(BackgroundCtnr);

            /** Progress bar **/
            LoginProgress = new UILoginProgress();
            LoginProgress.X = (ScreenWidth - (LoginProgress.Width + 20));
            LoginProgress.Y = (ScreenHeight - (LoginProgress.Height + 20));
            LoginProgress.Opacity = 0.9f;
            this.Add(LoginProgress);

            /** Login dialog **/
            LoginDialog = new UILoginDialog(this);
            LoginDialog.Opacity = 0.9f;
            //Center
            LoginDialog.X = (ScreenWidth - LoginDialog.Width) / 2;
            LoginDialog.Y = (ScreenHeight - LoginDialog.Height) / 2;
            this.Add(LoginDialog);

            NetworkFacade.Controller.OnNetworkError += new NetworkErrorDelegate(Controller_OnNetworkError);
            NetworkFacade.Controller.OnLoginProgress += new OnProgressDelegate(Controller_OnLoginProgress);
            NetworkFacade.Controller.OnLoginStatus += new OnLoginStatusDelegate(Controller_OnLoginStatus);
            var gameplayButton = new UIButton()
            {
                Caption = "Simantics & Lot Debug",
                Y = 10,
                Width = 200,
                X = 10
            };
            this.Add(gameplayButton);
            gameplayButton.OnButtonClick += new ButtonClickDelegate(gameplayButton_OnButtonClick);
        }
 
        void gameplayButton_OnButtonClick(UIElement button)
        {
            GameFacade.Controller.ShowLotDebug();
        }

        ~LoginScreen()
        {
            NetworkFacade.Controller.OnNetworkError -= new NetworkErrorDelegate(Controller_OnNetworkError);
            NetworkFacade.Controller.OnLoginProgress -= new OnProgressDelegate(Controller_OnLoginProgress);
            NetworkFacade.Controller.OnLoginStatus -= new OnLoginStatusDelegate(Controller_OnLoginStatus);
        }

        private void Controller_OnLoginProgress(TSOClient.Network.Events.ProgressEvent e)
        {
            var stage = e.Done;

            LoginProgress.ProgressCaption = GameFacade.Strings.GetString("210", (stage + 3).ToString());
            LoginProgress.Progress = 25 * stage;
        }

        private void Controller_OnLoginStatus(TSOClient.Network.Events.LoginEvent e)
        {
            m_InLogin = false;
            if (e.Success)
            {
                /** Save the username **/
                GlobalSettings.Default.LastUser = LoginDialog.Username;
                GlobalSettings.Default.Save();
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
        public void Login()
        {
            if (m_InLogin) { return; }
            m_InLogin = true;

            PlayerAccount.Username = LoginDialog.Username;
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
