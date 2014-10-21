/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Rhys Simpson. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.LUI;
using TSOClient.Code.UI.Controls;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.UI.Panels
{
    public class UISelectHouseView : UIContainer
    {
        public event HouseViewSelection OnModeSelection;

        private UIImage Background;
        public UIButton WallsDownButton { get; set; }
        public UIButton WallsUpButton { get; set; }
        public UIButton WallsCutawayButton { get; set; }
        public UIButton RoofButton { get; set; }
        public Texture2D BackgroundImage { get; set; }

        public UISelectHouseView()
        {
            var script = this.RenderScript("selecthouseview.uis");

            Background = new UIImage(BackgroundImage);
            this.AddAt(0, Background);

            WallsDownButton.OnButtonClick += new ButtonClickDelegate(WallsDownClick);
            WallsUpButton.OnButtonClick += new ButtonClickDelegate(WallsUpClick);
            WallsCutawayButton.OnButtonClick += new ButtonClickDelegate(WallsCutClick);
            RoofButton.OnButtonClick += new ButtonClickDelegate(RoofClick);
        }

        void RoofClick(UIElement button)
        {
            OnModeSelection(3);
        }

        void WallsCutClick(UIElement button)
        {
            OnModeSelection(1);
        }

        void WallsUpClick(UIElement button)
        {
            OnModeSelection(2);
        }

        void WallsDownClick(UIElement button)
        {
            OnModeSelection(0);
        }

    }

    public delegate void HouseViewSelection(int mode);
}
