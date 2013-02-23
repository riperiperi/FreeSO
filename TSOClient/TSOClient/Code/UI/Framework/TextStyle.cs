using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;

namespace TSOClient.Code.UI.Framework
{
    public class TextStyle
    {
        public static TextStyle DefaultTitle;
        public static TextStyle DefaultLabel;
        public static TextStyle DefaultButton;

        public Font Font;
        private int m_pxSize;

        /// <summary>
        /// PX size
        /// </summary>
        public int Size
        {
            get
            {
                return m_pxSize;
            }
            set
            {
                m_pxSize = value;

                var bestFont = Font.GetNearest(m_pxSize);
                SpriteFont = bestFont.Font;
                Scale = ((float)m_pxSize) / ((float)bestFont.Size);
            }
        }


        public SpriteFont SpriteFont { get; internal set; }
        public float Scale { get; internal set; }


        public Color Color = Color.Wheat;
    }

    [Flags]
    public enum TextAlignment
    {
        Left = 0xF,
        Center = 0xF0,
        Right = 0xF00,
        Top = 0xF000,
        Middle = 0xF0000,
        Bottom = 0xF00000
    }
}
