using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.UI.Panels;

namespace TSOClient.Code.UI.Screens
{
    public class LoginScreen : GameScreen
    {
        private UIContainer BackgroundCtnr;
        private UIImage Background;
        private UILoginDialog LoginDialog;

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


            LoginDialog = new UILoginDialog();
            LoginDialog.Opacity = 0.8f;
            LoginDialog.Caption = "Login to The Sims Online";
            //Center
            LoginDialog.X = (ScreenWidth - LoginDialog.Width) / 2;
            LoginDialog.Y = (ScreenHeight - LoginDialog.Height) / 2;
            this.Add(LoginDialog);


            var txtBox = new UITextEdit();
            txtBox.SetSize(400, 400);
            txtBox.X = 50;
            txtBox.Y = 50;
            txtBox.CurrentText = "This is a very long piece of text, i have put it in to test the word wrapping functionality";
            this.Add(txtBox);

            
        }
    }
}
