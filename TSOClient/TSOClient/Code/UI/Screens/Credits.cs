/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Controls;
using TSOClient.LUI;
using TSOClient.Code.UI.Framework.Parser;

namespace TSOClient.Code.UI.Screens
{
    public class Credits : GameScreen
    {
        public Texture2D BackgroundImage { get; set; }
        public UIButton BackButton { get; set; }
        public UIButton OkButton { get; set; }

        public Credits()
        {
            var ui = this.RenderScript("credits.uis");

            if (GlobalSettings.Default.ScaleUI)
            {
                this.ScaleX = this.ScaleY = ScreenWidth / 800.0f;
            }
            else
            {
                this.X = (float)((double)(ScreenWidth - 800)) / 2;
                this.Y = (float)((double)(ScreenHeight - 600)) / 2;
            }



            this.AddAt(0, new UIImage(BackgroundImage));
            this.Add(ui.Create<UIImage>("TSOLogoImage"));


            BackButton.OnButtonClick += new ButtonClickDelegate(BackButton_OnButtonClick);
            OkButton.OnButtonClick += new ButtonClickDelegate(BackButton_OnButtonClick);
        }

        void BackButton_OnButtonClick(UIElement button)
        {
            GameFacade.Screens.RemoveScreen(this);
        }
    }
}
