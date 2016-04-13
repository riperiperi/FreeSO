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

        private int[] OldMotives = new int[8];
        private int[] ArrowStates = new int[8];
        private int[] TargetArrowStates = new int[8];
        private int MotiveTick;
        private bool FirstFrame = true;

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

            var arrowState = ArrowStates[motive];
            var arrow = TextureGenerator.GetMotiveArrow(batch.GraphicsDevice, Color.White, Color.Transparent);
            if (arrowState > 0)
            {
                for (int i = 0; i < Math.Ceiling(arrowState / 60f); i++)
                    DrawLocalTexture(batch, arrow, new Rectangle(2, 0, 3, 5), new Vector2(x + 61 + i*4, y), new Vector2(1, 1), new Color(0x00, 0xCB, 0x39) * Math.Min(1f, arrowState/60f-i));
            } else if (arrowState < 0)
            {
                arrowState = -arrowState;
                for (int i = 0; i < Math.Ceiling(arrowState / 60f); i++)
                    DrawLocalTexture(batch, arrow, new Rectangle(0, 0, 3, 5), new Vector2(x-4 - i * 4, y), new Vector2(1, 1), new Color(0xD6, 0x00, 0x00) * Math.Min(1f, arrowState / 60f - i));
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            //TODO: remember a tick history to reduce the delay between a motive change and the arrows updating
            if (++MotiveTick > 180)
            {
                for (int i = 0; i < 8; i++)
                {
                    if (!FirstFrame)
                    {
                        var diff = (MotiveValues[i] - OldMotives[i]) / 2.5;
                        if (diff < 0) diff = Math.Floor(diff);
                        else if (diff > 0) diff = Math.Ceiling(diff);
                        TargetArrowStates[i] = Math.Max(Math.Min((int)diff, 5), -5) * 60;
                    }
                    OldMotives[i] = MotiveValues[i];
                }
                FirstFrame = false;
                MotiveTick = 0;
            }
            for (int i=0; i<8; i++)
            {
                if (TargetArrowStates[i] > ArrowStates[i]) ArrowStates[i]++;
                else if (TargetArrowStates[i] < ArrowStates[i]) ArrowStates[i]--;
            }
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
