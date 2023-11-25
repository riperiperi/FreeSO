using FSO.Client.UI.Framework;
using FSO.Client.UI.Framework.Parser;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace FSO.Client.UI
{
    public class UIRelationshipBar : UIElement
    {
        public static Texture2D G_BGLeft;
        public static Texture2D G_BGCtr;
        public static Texture2D G_BGRight;
        public static Texture2D G_Bar;

        public static Texture2D R_BGLeft;
        public static Texture2D R_BGCtr;
        public static Texture2D R_BGRight;
        public static Texture2D R_Bar;

        public int Value = 50;

        private int m_Width;
        private int m_Height;

        public float Width
        {
            get { return m_Width; }
            set
            {
                m_Width = (int)value;
            }
        }

        public float Height
        {
            get { return m_Height; }
            set
            {
                m_Height = (int)value;
            }
        }

        [UIAttribute("size")]
        public override Vector2 Size
        {
            get
            {
                return new Vector2(m_Width, m_Height);
            }
            set
            {
                Width = value.X;
                Height = value.Y;
            }
        }

        public UIRelationshipBar()
        {
            if (G_BGLeft == null)
            {
                G_BGLeft = GetTexture(0x1E600000002);
                G_BGCtr = GetTexture(0x1E500000002);
                G_BGRight = GetTexture(0x1E700000002);
                G_Bar = GetTexture(0x1D100000002);

                R_BGLeft = GetTexture(0x1E900000002);
                R_BGCtr = GetTexture(0x1E800000002);
                R_BGRight = GetTexture(0x1EA00000002);
                R_Bar = GetTexture(0x1D200000002);
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) return;
            var green = Value >= 0;
            var left = green ? G_BGLeft : R_BGLeft;
            var ctr = green ? G_BGCtr : R_BGCtr;
            var right = green ? G_BGRight : R_BGRight;
            var bar = green ? G_Bar : R_Bar;

            DrawLocalTexture(batch, left, new Vector2());
            DrawLocalTexture(batch, ctr, null, new Vector2(8, 0), new Vector2(m_Width-15, 1));
            DrawLocalTexture(batch, right, new Vector2(m_Width-7, 0));

            var interpWidth = (((m_Width - 16) * Math.Abs(Value)) / 100) + 16;

            DrawLocalTexture(batch, bar, new Rectangle(0, 0, 8, 15), new Vector2());
            DrawLocalTexture(batch, bar, new Rectangle(8, 0, 1, 15), new Vector2(8,0), new Vector2(interpWidth - 15, 1));
            DrawLocalTexture(batch, bar, new Rectangle(9, 0, 7, 15), new Vector2(interpWidth - 7,0));
        }
    }
}
