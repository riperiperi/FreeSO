/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
    public class UILiveMode : UIDestroyablePanel
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

        public override void Destroy()
        {
            //nothing to detach from here
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
