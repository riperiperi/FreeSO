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
        private UIImage Background;
        private UILoginDialog LoginDialog;

        public LoginScreen()
        {
            /**
             * Scale the whole screen to 1024
             */
            //this.ScaleX = 1.28f;
            //this.ScaleY = 1.28f;
            
            /** Background image **/
            Background = new UIImage(GetTexture(0x3a3, 0x001));
            Background.ID = "Background";
            this.Add(Background);


            LoginDialog = new UILoginDialog();
            LoginDialog.Opacity = 0.8f;
            LoginDialog.Caption = "Login to The Sims Online";
            this.Add(LoginDialog);


            //var r1 = new UIRectangle();
            //r1.X = 30;
            //r1.Y = 160;
            //Add(r1);

            //r1 = new UIRectangle();
            //r1.X = 20;
            //r1.Y = 150;
            //Add(r1);


            //r1 = new UIRectangle();
            //r1.X = 90;
            //r1.Y = 90;
            //Add(r1);


            //r1 = new UIRectangle();
            //r1.X = 90;
            //r1.Y = 500;
            //Add(r1);


            var lbl = new UILabel();
            lbl.Caption = "Version 1.1097.1.0";
            lbl.X = 20;
            lbl.Y = 558;
            lbl.FontColor = new Microsoft.Xna.Framework.Graphics.Color(0xEF, 0xE3, 0x94);
            lbl.ScaleX = lbl.ScaleY = 1.1f;

            this.Add(lbl);
            
        }
    }
}
