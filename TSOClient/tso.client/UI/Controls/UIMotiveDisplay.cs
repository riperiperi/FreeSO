/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.UI.Model;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Rendering.Framework.IO;
using FSO.Client.Utils;
using FSO.Common.Utils;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// The motive display used in live mode. Labels, values and increment rate indicators can be custom set.
    /// </summary>
    public class UIMotiveDisplay : UIElement
    {
        public short[] MotiveValues;
        public string[] MotiveNames = {
        "Hunger",
        "Comfort",
        "Hygiene",
        "Bladder",
        "Energy",
        "Fun",
        "Social",
        "Room"
        };
        private Texture2D Filler;

        public UIMotiveDisplay()
        {
            MotiveValues = new short[8];
            Filler = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, Color.White);
        }

        private void DrawMotive(UISpriteBatch batch, int x, int y, int motive)
        {
            double p = (MotiveValues[motive]+100)/200.0;
            Color barcol = new Color((byte)(57 * (1 - p)), (byte)(213 * p + 97 * (1 - p)), (byte)(49 * p + 90 * (1 - p)));
            Color bgcol = new Color((byte)(57 * p + 214*(1-p)), (byte)(97 * p), (byte)(90 * p));

            batch.Draw(Filler, LocalRect(x, y, 60, 5), bgcol);
            batch.Draw(Filler, LocalRect(x, y, (int)(60*p), 5), barcol);
            batch.Draw(Filler, LocalRect(x+(int)(60 * p), y, 1, 5), Color.Black); 
            var style = TextStyle.DefaultLabel.Clone();
            style.Size = 8;

            var temp = style.Color;
            style.Color = Color.Black;
            DrawLocalString(batch, MotiveNames[motive], new Vector2(x+1, y - 12), style, new Rectangle(0, 0, 60, 12), TextAlignment.Center); //shadow

            style.Color = temp;
            DrawLocalString(batch, MotiveNames[motive], new Vector2(x, y - 13), style, new Rectangle(0, 0, 60, 12), TextAlignment.Center);
        }

        public override void Draw(UISpriteBatch batch)
        {
            for (int i = 0; i < 4; i++)
            {
                DrawMotive(batch, 20, 13 + 20 * i, i); //left side
                DrawMotive(batch, 120, 13 + 20 * i, i+4); //right side
            }
        }
    }
}
