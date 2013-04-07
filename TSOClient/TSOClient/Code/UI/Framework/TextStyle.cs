using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Framework.Parser;
using Microsoft.Xna.Framework;
using TSOClient.Code.UI.Model;

namespace TSOClient.Code.UI.Framework
{
    public class TextStyle : UIAttributeParser
    {
        public static TextStyle DefaultTitle;
        public static TextStyle DefaultLabel;
        public static TextStyle DefaultButton;

        public Font Font;
        private int m_pxSize;


        public TextStyle Clone()
        {
            return new TextStyle
            {
                Color = this.Color,
                CursorColor = this.CursorColor,
                Font = this.Font,
                SelectedColor = this.SelectedColor,
                SelectionBoxColor = this.SelectionBoxColor,
                Size = this.Size,
                DisabledColor = this.DisabledColor,
                HighlightedColor = this.HighlightedColor
            };
        }

        public Color GetColor(UIElementState state)
        {
            switch (state)
            {
                case UIElementState.Normal:
                    return Color;
                case UIElementState.Highlighted:
                    return HighlightedColor;
                case UIElementState.Selected:
                    return SelectedColor;
                case UIElementState.Disabled:
                    return DisabledColor;
            }

            return Color;
        }

        public Vector2 MeasureString(string text)
        {
            var result = SpriteFont.MeasureString(text);
            return result * Scale;
        }

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



        /**
         * 
		     * textColor = (255,249,157) 
		     * textColorSelected = (0,243,247)
         */

        public Color Color = new Color(255, 249, 157);
        public Color SelectedColor = new Color(0x00, 0x38, 0x7B);
        public Color DisabledColor = new Color(100, 100, 100);
        public Color HighlightedColor = new Color(0xFF, 0xFF, 0xFF);

        public Color SelectionBoxColor = new Color(255, 249, 157);
        public Color CursorColor = new Color(255, 249, 157);


        #region UIAttributeParser Members

        void UIAttributeParser.ParseAttribute(UINode node)
        {
            /**
             * font = 10
		     * textColor = (255,249,157) 
		     * textColorSelected = (0,243,247)
		     * textColorHighlighted = (255,255,255)	
		     * textColorDisabled = (100,100,100)
             */

            var fontSize = 10;
            if (node.Attributes.ContainsKey("font"))
            {
                fontSize = int.Parse(node.Attributes["font"]);
            }

            var fontColor = TextStyle.DefaultButton.Color;
            if (node.Attributes.ContainsKey("textColor"))
            {
                fontColor = UIScript.ParseRGB(node.Attributes["textColor"]);
            }
            if (node.Attributes.ContainsKey("color"))
            {
                fontColor = UIScript.ParseRGB(node.Attributes["color"]);
            }

            if (node.Attributes.ContainsKey("textColorSelected"))
            {
                SelectedColor = UIScript.ParseRGB(node.Attributes["textColorSelected"]);
            }
            if (node.Attributes.ContainsKey("textColorHighlighted"))
            {
                HighlightedColor = UIScript.ParseRGB(node.Attributes["textColorHighlighted"]);
            }
            if (node.Attributes.ContainsKey("textColorDisabled"))
            {
                DisabledColor = UIScript.ParseRGB(node.Attributes["textColorDisabled"]);
            }


            if (node.Attributes.ContainsKey("cursorColor"))
            {
                CursorColor = UIScript.ParseRGB(node.Attributes["cursorColor"]);
            }

            Font = GameFacade.MainFont;
            Size = fontSize;
            Color = fontColor;

        }

        #endregion
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
