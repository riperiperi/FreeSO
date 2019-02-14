/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework.Parser;
using Microsoft.Xna.Framework;
using FSO.Client.UI.Model;
using FSO.UI.Framework;

namespace FSO.Client.UI.Framework
{
    public class TextStyle : UIAttributeParser
    {
        public static TextStyle DefaultTitle;
        public static TextStyle DefaultLabel;
        public static TextStyle DefaultButton;

        public Font Font;
        public MSDFFont VFont;
        public bool Shadow; //some text has a shadow
        private int m_pxSize;

        /// <summary>
        /// Offset in pixels of the baseline
        /// </summary>
        public float BaselineOffset
        {
            get;
            set;
        }

        public TextStyle Clone()
        {
            return new TextStyle
            {
                Color = this.Color,
                CursorColor = this.CursorColor,
                Font = this.Font,
                VFont = this.VFont,
                SelectedColor = this.SelectedColor,
                SelectionBoxColor = this.SelectionBoxColor,
                Size = this.Size,
                Shadow = this.Shadow,
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
            if (VFont != null)
            {
                var result = VFont.MeasureString(text);
                return result * Scale;
            }
            else
            {
                SpriteFont.DefaultCharacter = '*';
                var result = SpriteFont.MeasureString(text);
                return result * Scale;
            }
        }

        public string TruncateToWidth(string text, int width)
        {
            return TruncateToWidth(text, width, "...");
        }

        public string TruncateToWidth(string text, int width, string truncator)
        {
            if (MeasureString(text).X <= width) return text;
            while (text.Length > 0)
            {
                text = text.Substring(0, text.Length - 1);
                if (MeasureString(text+truncator).X <= width) return text + truncator;
            }
            return text;
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


                if (VFont != null)
                {
                    Scale = (m_pxSize * VFont.VectorScale) / 11.5f;
                    BaselineOffset = m_pxSize + 5;
                }
                else
                {
                    var bestFont = Font.GetNearest(m_pxSize);
                    SpriteFont = bestFont.Font;
                    Scale = ((float)m_pxSize) / ((float)bestFont.Size);
                    BaselineOffset = m_pxSize + 5 + (float)Math.Floor(((m_pxSize + 5) * Scale));
                }
            }
        }

        public int LineHeightModifier;

        private int? _LineHeight;

        public int LineHeight
        {
            get
            {
                if(_LineHeight == null)
                {
                    _LineHeight = (int)MeasureString("D").Y;
                }

                return _LineHeight.Value + LineHeightModifier;
            }
        }

        public SpriteFont SpriteFont { get; internal set; }
        public float Scale { get; set; }



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
            VFont = GameFacade.VectorFont;
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
