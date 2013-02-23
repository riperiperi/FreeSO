using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Controls;

namespace TSOClient.Code.UI.Screens
{
    public class LoadingScreen : GameScreen
    {
        private UIImage Background;

        public LoadingScreen()
        {
            /**
             * Scale the whole screen to 1024
             */
            this.ScaleX = 1.28f;
            this.ScaleY = 1.28f;
            
            /** Background image **/
            Background = new UIImage(GetTexture(0x3a3, 0x001));
            this.Add(Background);

            var lbl = new UILabel();
            lbl.Caption = "(c) 2002, 2003 Electronic Arts Inc. All rights reserved.";
            lbl.X = 110;
            lbl.Y = 510;

            this.Add(lbl);
            
        }
    }
}
