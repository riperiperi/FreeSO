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
using Microsoft.Xna.Framework.Graphics;
using TSOClient.LUI;
using TSOClient.Code.UI.Controls;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Model;
using TSOClient.Code.UI.Screens;
using TSOClient.Code.Utils;

namespace TSOClient.Code.UI.Panels
{
    public class UIInbox : UIContainer
    {
        public UIListBox InboxListBox { get; set; }
        private UIImage Background;
        public Texture2D backgroundImage { get; set; }
        public Texture2D maxisIconImage { get; set; }
        public UIButton CloseButton { get; set; }
        private UIInboxDropdown Dropdown;

        public UIInbox()
        {
            var script = this.RenderScript("messageinbox.uis");

            Background = new UIImage(backgroundImage);
            this.AddAt(0, Background);
            CloseButton.OnButtonClick += new ButtonClickDelegate(Close);
            UIUtils.MakeDraggable(Background, this);

            var msgStyleCSR = script.Create<UIListBoxTextStyle>("CSRMessageColors", InboxListBox.FontStyle);
            var msgStyleServer = script.Create<UIListBoxTextStyle>("ServerMessageColors", InboxListBox.FontStyle);
            var msgStyleGame = script.Create<UIListBoxTextStyle>("GameMessageColors", InboxListBox.FontStyle);
            var msgStyleSim = script.Create<UIListBoxTextStyle>("SimMessageColors", InboxListBox.FontStyle);
            var msgStyleClub = script.Create<UIListBoxTextStyle>("ClubMessageColors", InboxListBox.FontStyle);
            var msgStyleProperty = script.Create<UIListBoxTextStyle>("PropertyMessageColors", InboxListBox.FontStyle);
            var msgStyleNeighborhood = script.Create<UIListBoxTextStyle>("NeighborhoodMessageColors", InboxListBox.FontStyle);

            var item = new UIListBoxItem("idk", "!", "", "||", "", "21:21 - 4/2/2014", "", "The Sims Online", "", "Please stop remaking our game");
            item.CustomStyle = msgStyleSim;

            InboxListBox.Items.Add(item);
            Dropdown = new UIInboxDropdown();
            Dropdown.X = 162;
            Dropdown.Y = 13;
            this.Add(Dropdown);

        }

        private void Close(UIElement button)
        {
            var screen = (CoreGameScreen)Parent;
            screen.CloseInbox();
        }
    }

    public class UIInboxDropdown : UIContainer
    {
        public Texture2D backgroundCollapsedImage { get; set; }
        public Texture2D backgroundExpandedImage { get; set; }

        public UIButton DropDownButton { get; set; }

        public UIButton MenuScrollUpButton { get; set; }
        public UIButton MenuScrollDownButton { get; set; }
        public UISlider MenuSlider { get; set; }

        public UIListBox MenuListBox { get; set; }

        public UIImage Background;
        public bool open;

        public UIInboxDropdown()
        {
            this.RenderScript("messageinboxmenu.uis");
            Background = new UIImage(backgroundCollapsedImage);
            this.AddAt(0, Background);

            open = true;
            ToggleOpen();

            DropDownButton.OnButtonClick += new ButtonClickDelegate(DropDownButton_OnButtonClick);

        }

        void DropDownButton_OnButtonClick(UIElement button)
        {
            ToggleOpen();
        }

        public void ToggleOpen()
        {
            if (open)
            {
                Background.Texture = backgroundCollapsedImage;
                Background.SetSize(backgroundCollapsedImage.Width, backgroundCollapsedImage.Height);
            }
            else
            {
                Background.Texture = backgroundExpandedImage;
                Background.SetSize(backgroundExpandedImage.Width, backgroundExpandedImage.Height);
            }
            open = !open;
            MenuSlider.Visible = open;
            MenuScrollUpButton.Visible = open;
            MenuScrollDownButton.Visible = open;
            MenuListBox.Visible = open;
        }
    }
}
