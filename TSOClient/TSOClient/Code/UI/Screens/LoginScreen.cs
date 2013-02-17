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
            this.ScaleX = 1.28f;
            this.ScaleY = 1.28f;
            
            /** Background image **/
            Background = new UIImage(GetTexture(0x3a3, 0x001));
            this.Add(Background);


            LoginDialog = new UILoginDialog();
            Add(LoginDialog);



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
