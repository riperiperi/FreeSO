using FSO.Client.UI.Controls;
using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.UI.Controls
{
    public class UIColorPicker : UIContainer
    {
        public float Hue;
        public float Saturation = 1f;
        public float Value = 1f;

        public Color Color
        {
            get
            {
                return TextureGenerator.FromHSV(Hue, Saturation, Value); 
            }

            set
            {
                
            }
        }

        private UIMouseEventRef ClickHandler;
        private bool MouseDown;

        private UITextBox RedText;
        private UITextBox GreenText;
        private UITextBox BlueText;
        private UITextBox HexText;
        private int InternalChange;

        public UIColorPicker()
        {
            ClickHandler =
                ListenForMouse(new Rectangle(0, 0, 206, 128), new UIMouseEvent(OnMouseEvent));

            RedText = new UITextBox();
            RedText.SetBackgroundTexture(null, 0, 0, 0, 0);
            RedText.SetSize(40, 22);
            RedText.Position = new Vector2(232, 50);
            RedText.OnChange += RecalcColor;
            Add(RedText);

            var redLabel = new UILabel();
            redLabel.Position = RedText.Position - new Vector2(20, -4);
            redLabel.Caption = "R:";
            Add(redLabel);

            GreenText = new UITextBox();
            GreenText.SetBackgroundTexture(null, 0, 0, 0, 0);
            GreenText.SetSize(40, 22);
            GreenText.Position = new Vector2(232, 50+28);
            GreenText.OnChange += RecalcColor;
            Add(GreenText);

            var greenLabel = new UILabel();
            greenLabel.Position = GreenText.Position - new Vector2(20, -4);
            greenLabel.Caption = "G:";
            Add(greenLabel);

            BlueText = new UITextBox();
            BlueText.SetBackgroundTexture(null, 0, 0, 0, 0);
            BlueText.SetSize(40, 22);
            BlueText.Position = new Vector2(232, 50+28*2);
            BlueText.OnChange += RecalcColor;
            Add(BlueText);

            var blueLabel = new UILabel();
            blueLabel.Position = BlueText.Position - new Vector2(20, -4);
            blueLabel.Caption = "B:";
            Add(blueLabel);

            HexText = new UITextBox();
            HexText.SetBackgroundTexture(null, 0, 0, 0, 0);
            HexText.SetSize(68, 45);
            HexText.Alignment = TextAlignment.Center | TextAlignment.Middle;
            HexText.TextStyle = HexText.TextStyle.Clone();
            HexText.TextStyle.Shadow = true;
            HexText.TextStyle.Size = 8;
            HexText.Position = new Vector2(208, 0);
            HexText.OnChange += HexText_OnChange;
            Add(HexText);

            InternalChange = 1;
            RecalcColor(null);
            InternalChange = 0;
        }

        private void HexText_OnChange(UIElement element)
        {
            if (InternalChange == 0)
            {
                //try parse what the user put here
                //if we cant just do nothing

                var hex = HexText.CurrentText.TrimStart('#');
                int num;
                if (int.TryParse(hex, NumberStyles.HexNumber,
                    CultureInfo.CurrentCulture, out num))
                {
                    var hsv = TextureGenerator.ToHSV(new Color((byte)(num >> 16), (byte)(num >> 8), (byte)num, (byte)255));
                    Hue = hsv.Item1;
                    Saturation = hsv.Item2;
                    Value = hsv.Item3;
                    InternalChange = 2;
                    RecalcColor(null);
                    InternalChange = 0;
                }
            }
        }

        private void RecalcColor(UIElement element)
        {
            var success = false;
            if (element != null)
            {
                if (InternalChange != 0) return;
                byte r, g, b;
                var rtext = RedText.CurrentText;
                if (rtext == "") rtext = "0";
                if (byte.TryParse(rtext, out r))
                {
                    var gtext = GreenText.CurrentText;
                    if (gtext == "") gtext = "0";
                    if (byte.TryParse(gtext, out g))
                    {
                        var btext = BlueText.CurrentText;
                        if (btext == "") btext = "0";
                        if (byte.TryParse(btext, out b))
                        {
                            var hsv = TextureGenerator.ToHSV(new Color(r, g, b, (byte)255));
                            Hue = hsv.Item1;
                            Saturation = hsv.Item2;
                            Value = hsv.Item3;
                            success = true;
                        }
                    }
                }
            }

            if (!success)
            {
                var col = Color;
                RedText.CurrentText = col.R.ToString();
                GreenText.CurrentText = col.G.ToString();
                BlueText.CurrentText = col.B.ToString();
                if (InternalChange < 2)
                {
                    var lasti = InternalChange;
                    InternalChange = 1;
                    HexText.CurrentText = "#" + ((col.R << 16) | (col.G << 8) | (col.B)).ToString("X6");
                    InternalChange = lasti;
                }
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (MouseDown)
            {
                var changed = false;
                var relPos = GlobalPoint(state.MouseState.Position.ToVector2());
                relPos.X = Math.Min(186 + 20, Math.Max(0, relPos.X));
                relPos.Y = Math.Min(128, Math.Max(0, relPos.Y));
                if (relPos.X < 180)
                {
                    Hue = relPos.X * 2f;
                    Saturation = 1 - (relPos.Y / 128f);
                    changed = true;
                } else if (relPos.X > 186)
                {
                    Value = 1 - (relPos.Y / 128f);
                    changed = true;
                }

                if (changed)
                {
                    InternalChange = 1;
                    RecalcColor(null);
                    InternalChange = 0;
                }
            }
        }

        public void OnMouseEvent(UIMouseEventType type, UpdateState state)
        {
            switch (type)
            {
                case UIMouseEventType.MouseDown:
                    MouseDown = true;
                    break;
                case UIMouseEventType.MouseUp:
                    MouseDown = false;
                    break;
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            var col = TextureGenerator.GetHSMatrix(batch.GraphicsDevice);
            var mod = Color.White * Value;
            mod.A = 255;
            DrawLocalTexture(batch, col, null, Vector2.Zero, new Vector2(0.5f), mod);
            var value = TextureGenerator.GetHSGrad(batch.GraphicsDevice);
            DrawLocalTexture(batch, value, null, new Vector2(186, 0), new Vector2(20, 0.5f), TextureGenerator.FromHSV(Hue, Saturation, 1f));

            var targ = TextureGenerator.GetSun(batch.GraphicsDevice);
            DrawLocalTexture(batch, targ, null, new Vector2(Hue / 2 - 2, (1-Saturation) * 128 - 2), new Vector2(4/256f), Color.White);

            var col2 = Color;
            var px = TextureGenerator.GetPxWhite(batch.GraphicsDevice);
            DrawLocalTexture(batch, px, null, new Vector2(183, (1-Value) * 128 - 1.5f), new Vector2(26, 3), Color.White);
            DrawLocalTexture(batch, px, null, new Vector2(212, 0), new Vector2(60, 45), Color.White);
            DrawLocalTexture(batch, px, null, new Vector2(213, 1), new Vector2(58, 43), col2);

            DrawLocalTexture(batch, px, null, RedText.Position, RedText.Size, new Color(0x00, 0x33, 0x66) * 0.75f);
            DrawLocalTexture(batch, px, null, GreenText.Position, GreenText.Size, new Color(0x00, 0x33, 0x66) * 0.75f);
            DrawLocalTexture(batch, px, null, BlueText.Position, BlueText.Size, new Color(0x00, 0x33, 0x66) * 0.75f);

            var avg = (col2.R + col2.G + col2.B) / 3;
            HexText.TextStyle.Color = (avg > 196) ? Color.Black : Color.White;
            HexText.TextStyle.Shadow = (avg <= 196);



            base.Draw(batch);
        }
    }
}
