/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
RHY3756547. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.LUI;
using TSOClient.Code.UI.Controls;
using Microsoft.Xna.Framework;
using TSO.Simantics;
using TSO.Simantics.model;

namespace TSOClient.Code.UI.Panels
{
    /// <summary>
    /// Live Mode Panel
    /// </summary>
    public class UILiveMode : UIContainer
    {
        public UIImage Background;
        public UIImage Divider;
        public UIMotiveDisplay MotiveDisplay;
        public Texture2D DividerImg { get; set; }
        public Texture2D PeopleListBackgroundImg { get; set; }

        //EOD buttons
        public UIButton EODHelpButton { get; set; }
        public UIButton EODCloseButton { get; set; }
        public UIButton EODExpandButton { get; set; }
        public UIButton EODContractButton { get; set; }

        public TSO.Simantics.VM vm;
        public VMAvatar SelectedAvatar;


        public UILiveMode () {
            var script = this.RenderScript("livepanel"+((GlobalSettings.Default.GraphicsWidth < 1024)?"":"1024")+".uis");

            Background = new UIImage(GetTexture((GlobalSettings.Default.GraphicsWidth < 1024) ? (ulong)0x000000D800000002 : (ulong)0x0000018300000002));
            Background.Y = 33;
            this.AddAt(0, Background);

            var PeopleListBg = new UIImage(PeopleListBackgroundImg);
            PeopleListBg.Position = new Microsoft.Xna.Framework.Vector2(375, 38);
            this.AddAt(1, PeopleListBg);

            Divider = new UIImage(DividerImg);
            Divider.Position = new Microsoft.Xna.Framework.Vector2(140, 49);
            this.AddAt(1, Divider);

            MotiveDisplay = new UIMotiveDisplay();
            MotiveDisplay.Position = new Vector2(165, 59);
            this.Add(MotiveDisplay);

            EODHelpButton.Visible = false;
            EODCloseButton.Visible = false;
            EODExpandButton.Visible = false;
            EODContractButton.Visible = false;
        }

        public override void Update(TSO.Common.rendering.framework.model.UpdateState state)
        {
            base.Update(state);
            if (SelectedAvatar != null) UpdateMotives();
        }

        private void UpdateMotives()
        {
            MotiveDisplay.MotiveValues[0] = SelectedAvatar.GetMotiveData(VMMotive.Hunger);
            MotiveDisplay.MotiveValues[1] = SelectedAvatar.GetMotiveData(VMMotive.Comfort);
            MotiveDisplay.MotiveValues[2] = SelectedAvatar.GetMotiveData(VMMotive.Hygiene);
            MotiveDisplay.MotiveValues[3] = SelectedAvatar.GetMotiveData(VMMotive.Bladder);
            MotiveDisplay.MotiveValues[4] = SelectedAvatar.GetMotiveData(VMMotive.Energy);
            MotiveDisplay.MotiveValues[5] = SelectedAvatar.GetMotiveData(VMMotive.Fun);
            MotiveDisplay.MotiveValues[6] = SelectedAvatar.GetMotiveData(VMMotive.Social);
            MotiveDisplay.MotiveValues[7] = SelectedAvatar.GetMotiveData(VMMotive.Room);
        }
    }
}
