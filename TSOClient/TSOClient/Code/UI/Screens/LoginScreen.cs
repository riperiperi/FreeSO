using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.UI.Panels;
using TSOClient.Code.Network;

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
        }

        private bool m_InLogin = false;
        /// <summary>
        /// Called by login button click in UILoginDialog
        /// </summary>
        public void Login(){
            if (m_InLogin) { return; }
            m_InLogin = true;
            Async(new AsyncHandler(DoLogin));
        }

        private void DoLogin() 
        {
            NetworkFacade.LoginProgress += new LoginProgressDelegate(NetworkFacade_LoginProgress);
            NetworkFacade.Controller.OnNetworkError += new TSOClient.Network.NetworkErrorDelegate(Controller_OnNetworkError);
            NetworkFacade.Controller.InitialConnect(LoginDialog.Username.ToUpper(), LoginDialog.Password.ToUpper());

            NetworkFacade.LoginWait.WaitOne();

            if (NetworkFacade.LoginOK == false)
            {
                /** Reset **/
                LoginProgress.ProgressCaption = GameFacade.Strings.GetString("210", "4");
                LoginProgress.Progress = 0;
                m_InLogin = false;
            }
            else
            {
                /** Go to the select a sim page, make sure we do this in the UIThread **/
                GameFacade.Controller.ShowPersonSelection();
            }
        }

        /// <summary>
        /// A network error occured - 95% of the time, this will be because
        /// a connection could not be established.
        /// </summary>
        /// <param name="Exception">The exception that occured.</param>
        private void Controller_OnNetworkError(SocketException Exception)
        {
            UIAlertOptions Options = new UIAlertOptions();
            Options.Message = "Couldn't connect! Server is busy or down.";
            Options.Title = "Network error";
            Options.Buttons = UIAlertButtons.OK;
            UI.Framework.UIScreen.ShowAlert(Options, true);

            /** Reset **/
            LoginProgress.ProgressCaption = GameFacade.Strings.GetString("210", "4");
            LoginProgress.Progress = 0;
            m_InLogin = false;
        }

        private void NetworkFacade_LoginProgress(int stage)
        {
            LoginProgress.ProgressCaption = GameFacade.Strings.GetString("210", (stage + 4).ToString());
            LoginProgress.Progress = 25 * stage;
        }
    }
}
