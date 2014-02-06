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

        public UIInbox() {
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
        }

        private void Close(UIElement button)
        {
            var screen = (CoreGameScreen)Parent;
            screen.CloseInbox();
        }
    }
}
