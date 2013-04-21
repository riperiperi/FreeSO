using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
            Background = new UIImage(GetTexture(0x3a3, 0x001));
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

        private void DoLogin() {
            var loginResult = 
                NetworkFacade.Controller.InitialConnect(
                    LoginDialog.Username, 
                    LoginDialog.Password,
                    new LoginProgressDelegate(UpdateLoginProgress));

            if (loginResult == false)
            {
                /** Reset **/
                LoginProgress.ProgressCaption = GameFacade.Strings.GetString("210", "4");
                LoginProgress.Progress = 0;
            }
            else
            {
                /** Go to the select a sim page, make sure we do this in the UIThread **/
                GameFacade.Controller.ShowPersonSelection();
                //OnNextUpdate(new AsyncHandler(GameFacade.Controller.ShowPersonSelection));
            }
        }

        private void UpdateLoginProgress(int stage)
        {
            LoginProgress.ProgressCaption = GameFacade.Strings.GetString("210", (stage + 4).ToString());
            LoginProgress.Progress = 25 * stage;
        }



    }
}
