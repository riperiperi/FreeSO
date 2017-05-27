/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Controls;
using FSO.Client.UI.Screens;
using FSO.Client.Rendering.City;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels
{
    //in matchmaker displays title of city. in lot displays lot name.

    public class UIGameTitle : UICachedContainer
    {
        public UIImage Background;
        public UILabel Label;

        public UIGameTitle()
        {
            Background = new UIImage(GetTexture((ulong)0x000001A700000002));
            Background.With9Slice(40, 40, 0, 0);
            this.AddAt(0, Background);
            Background.BlockInput();

            Label = new UILabel();
            Label.CaptionStyle = TextStyle.DefaultLabel.Clone();
            Label.CaptionStyle.Size = 11;
            Label.Alignment = TextAlignment.Middle;
            this.Add(Label);

            SetTitle("Not Blazing Falls");
        }

        public void SetTitle(string title)
        {
            Label.Caption = title;

            var style = Label.CaptionStyle;

            var width = style.MeasureString(title).X;
            var ScreenWidth = GlobalSettings.Default.GraphicsWidth/2;

            X = ScreenWidth - (width / 2 + 40);
            Background.X = 0;
            Background.SetSize(width + 80, 24);
            Size = new Vector2(width + 80, 24);

            Label.X = 40;
            Label.Size = new Vector2(width, 20);

        }
    }
}
