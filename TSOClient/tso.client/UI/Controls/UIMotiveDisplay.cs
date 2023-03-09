/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.SimAntics.Model;
using FSO.SimAntics;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// The motive display used in live mode. Labels, values and increment rate indicators can be custom set.
    /// </summary>
    public class UIMotiveDisplay : UIElement
    {
        private short[] MotiveValues;
        private string[] MotiveNames;
        private Texture2D Filler;

        private int[] OldMotives = new int[8];
        private Queue<int>[] ChangeBuffer = new Queue<int>[8];
        private int[] ArrowStates = new int[8];
        private int[] TargetArrowStates = new int[8];
        private bool FirstFrame = true;
        private TextStyle MotiveStyle;

        public bool DynamicMode { get; set; }

        public UIMotiveDisplay()
        {
            MotiveNames = new string[8];
            for (int i=1; i<9; i++)
            {
                MotiveNames[i - 1] = GameFacade.Strings.GetString("f102", i.ToString());
            }
            MotiveValues = new short[8];
            Filler = TextureGenerator.GetPxWhite(GameFacade.GraphicsDevice);
            for (int i = 0; i < 8; i++) ChangeBuffer[i] = new Queue<int>();

            var style = TextStyle.DefaultLabel.Clone();
            style.Size = 8;
            MotiveStyle = style;
            MotiveStyle.Color = Color.White;
        }

        private void DrawMotive(UISpriteBatch batch, int x, int y, int motive, bool inDynamic)
        {
            if (DynamicMode || inDynamic)
            {
                var mdat = MotiveValues[motive] + 100;
                double p = Math.Max(0, Math.Min(1, (mdat) / 200.0));
                Color barcol = new Color((byte)(57 * (1 - p)), (byte)(213 * p + 97 * (1 - p)), (byte)(49 * p + 90 * (1 - p)));
                Color bgcol = new Color((byte)(57 * p + 214 * (1 - p)), (byte)(97 * p), (byte)(90 * p));

                batch.Draw(Filler, LocalRect(x, y, 60, 5), bgcol);
                batch.Draw(Filler, LocalRect(x, y, (int)(60 * p), 5), barcol);
                batch.Draw(Filler, LocalRect(x + (int)(60 * p), y, 1, 5), Color.Black);

                if (mdat > 200)
                {
                    var p2 = Math.Min(1, (mdat - 200) / 200.0);
                    batch.Draw(Filler, LocalRect(x, y, (int)(60 * p2), 5), Color.White);
                    batch.Draw(Filler, LocalRect(x + (int)(60 * p2), y, 1, 5), Color.Black);

                    //MotiveStyle.Shadow = true;
                    //DrawLocalString(batch, "+" + ((mdat-200) / 2f).ToString() + "%", new Vector2(x, y), MotiveStyle, new Rectangle(0, 0, 60, 5), TextAlignment.Center | TextAlignment.Middle);
                }

                if (mdat < 0)
                {
                    var p2 = Math.Min(1, (-mdat) / 200.0);
                    batch.Draw(Filler, LocalRect(x + 60 - (int)(60 * p2), y, (int)(60 * p2), 5), Color.Black);
                    batch.Draw(Filler, LocalRect(x + 60 - (int)(60 * p2), y, 1, 5), Color.White);

                    //MotiveStyle.Shadow = true;
                    //DrawLocalString(batch, "+" + ((mdat-200) / 2f).ToString() + "%", new Vector2(x, y), MotiveStyle, new Rectangle(0, 0, 60, 5), TextAlignment.Center | TextAlignment.Middle);
                }

                var arrowState = ArrowStates[motive];
                var arrow = TextureGenerator.GetMotiveArrow(batch.GraphicsDevice);
                if (arrowState > 0)
                {
                    for (int i = 0; i < Math.Ceiling(arrowState / 60f); i++)
                        DrawLocalTexture(batch, arrow, new Rectangle(2, 0, 3, 5), new Vector2(x + 61 + i * 4, y), new Vector2(1, 1), new Color(0x00, 0xCB, 0x39) * Math.Min(1f, arrowState / 60f - i));
                }
                else if (arrowState < 0)
                {
                    arrowState = -arrowState;
                    for (int i = 0; i < Math.Ceiling(arrowState / 60f); i++)
                        DrawLocalTexture(batch, arrow, new Rectangle(0, 0, 3, 5), new Vector2(x - 4 - i * 4, y), new Vector2(1, 1), new Color(0xD6, 0x00, 0x00) * Math.Min(1f, arrowState / 60f - i));
                }
            }
            
            if (DynamicMode || !inDynamic)
            {
                var style = MotiveStyle;

                var temp = TextStyle.DefaultLabel.Color;
                style.Color = Color.Black;
                DrawLocalString(batch, MotiveNames[motive], new Vector2(x + 1, y - 14), style, new Rectangle(0, 0, 60, 12), TextAlignment.Center); //shadow

                style.Color = temp;
                DrawLocalString(batch, MotiveNames[motive], new Vector2(x, y - 15), style, new Rectangle(0, 0, 60, 12), TextAlignment.Center);
            }
        }

        public void UpdateMotives(VMAvatar avatar, Func<int, short, short> transform, bool clearChange = false)
        {
            MotiveValues[0] = avatar.GetMotiveData(VMMotive.Hunger);
            MotiveValues[1] = avatar.GetMotiveData(VMMotive.Comfort);
            MotiveValues[2] = avatar.GetMotiveData(VMMotive.Hygiene);
            MotiveValues[3] = avatar.GetMotiveData(VMMotive.Bladder);
            MotiveValues[4] = avatar.GetMotiveData(VMMotive.Energy);
            MotiveValues[5] = avatar.GetMotiveData(VMMotive.Fun);
            MotiveValues[6] = avatar.GetMotiveData(VMMotive.Social);
            MotiveValues[7] = avatar.GetMotiveData(VMMotive.Room);

            if (transform != null)
            {
                for (int i = 0; i < 8; i++)
                {
                    MotiveValues[i] = transform(i, MotiveValues[i]);
                }
            }

            if (clearChange)
            {
                for (int i = 0; i < 8; i++)
                {
                    OldMotives[i] = MotiveValues[i];
                    ChangeBuffer[i].Clear();
                }
            }
        }

        private bool UpdateFlip;
        public override void Update(UpdateState state)
        {
            UpdateFlip = !UpdateFlip;
            if (UpdateFlip) return;
            base.Update(state);

            for (int i = 0; i < 8; i++)
            {
                if (!FirstFrame) ChangeBuffer[i].Enqueue(MotiveValues[i] - OldMotives[i]);
                if (ChangeBuffer[i].Count > 240) ChangeBuffer[i].Dequeue();

                int sum = 0;
                foreach (var c in ChangeBuffer[i]) sum += c;

                var diff = sum / 2.5;
                if (diff < 0) diff = Math.Floor(diff);
                else if (diff > 0) diff = Math.Ceiling(diff);
                TargetArrowStates[i] = Math.Max(Math.Min((int)diff, 5), -5) * 60;

                OldMotives[i] = MotiveValues[i];
            }
            FirstFrame = false;

            for (int i=0; i<8; i++)
            {
                if (TargetArrowStates[i] > ArrowStates[i]) ArrowStates[i]++;
                else if (TargetArrowStates[i] < ArrowStates[i]) ArrowStates[i]--;
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            var inDynamic = batch.GraphicsDevice.GetRenderTargets().Length == 0;
            for (int i = 0; i < 4; i++)
            {
                DrawMotive(batch, 20, 13 + 20 * i, i, inDynamic); //left side
                DrawMotive(batch, 120, 13 + 20 * i, i+4, inDynamic); //right side
            }
        }
    }
}
