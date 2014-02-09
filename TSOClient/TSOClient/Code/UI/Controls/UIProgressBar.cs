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
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using TSOClient.Code.UI.Model;
using TSOClient.Code.Utils;
using tso.common.utils;

namespace TSOClient.Code.UI.Controls
{
    /// <summary>
    /// 
    /// </summary>
    public class UIProgressBar : UIElement
    {
        public static ITextureRef StandardBackground;
        public static ITextureRef StandardBar;
        public static TextStyle StandardCaptionStyle;


        static UIProgressBar()
        {
            StandardBackground = new SlicedTextureRef(
                UIElement.GetTexture((ulong)TSOClient.FileIDs.UIFileIDs.dialog_progressbarback),
                new Microsoft.Xna.Framework.Rectangle(13, 13, 13, 13)
            );

            var barTexture = UIElement.GetTexture((ulong)TSOClient.FileIDs.UIFileIDs.dialog_progressbarfront);
            TextureUtils.ManualTextureMask(ref barTexture, new uint[1] { new Color(0x39, 0x51, 0x6B).PackedValue });

            StandardBar = new SlicedTextureRef(barTexture, new Rectangle(18, 7, 18, 7));

            StandardCaptionStyle = TextStyle.DefaultLabel.Clone();
            StandardCaptionStyle.Color = new Color(0, 0, 0);
        }



        private float m_Width;
        private float m_Height;


        public UIProgressBar() : this(StandardBackground, StandardBar)
        {
            BarMargin = new Rectangle(18, 0, 18, 0);
            CaptionStyle = StandardCaptionStyle;
        }

        public UIProgressBar(ITextureRef background, ITextureRef bar)
        {
            this.Background = background;
            this.Bar = bar;
        }

        public string Caption = "{0}%";
        public TextStyle CaptionStyle { get; set; }

        public ITextureRef Background { get; set; }
        public ITextureRef Bar { get; set; }
        public Rectangle BarMargin = Rectangle.Empty;
        public Rectangle BarOffset = Rectangle.Empty;

        private Rectangle m_Bounds = Rectangle.Empty;

        private float m_MinValue = 0;
        public float MinValue
        {
            get { return m_MinValue; }
            set
            {
                m_MinValue = value;
            }
        }

        private float m_MaxValue = 100;
        public float MaxValue
        {
            get { return m_MaxValue; }
            set
            {
                m_MaxValue = value;
            }
        }



        private float m_Value = 0;
        public float Value
        {
            get { return m_Value; }
            set
            {
                var newValue = value;
                newValue = Math.Min(newValue, m_MaxValue);
                newValue = Math.Max(newValue, m_MinValue);

                if (m_Value != newValue)
                {
                    m_Value = newValue;
                }
            }
        }
        

        




        public float Width
        {
            get { return m_Width; }
        }

        public float Height
        {
            get { return m_Height; }
        }

        public void SetSize(float width, float height)
        {
            m_Width = width;
            m_Height = height;
            m_Bounds = new Rectangle(0, 0, (int)width, (int)height);
        }


        public override void Draw(UISpriteBatch SBatch)
        {
            if (Background != null)
            {
                Background.Draw(SBatch, this, 0, 0, m_Width, m_Height);
            }

            
            var percent = (m_Value - m_MinValue) / (m_MaxValue - m_MinValue);

            if (m_Value != 0 && Bar != null)
            {
                /** Draw progress bar **/
                var trackSize = m_Width - BarOffset.Right - BarMargin.Right;
                var barWidth = BarMargin.Right + (trackSize * percent);
                var barHeight = m_Height - BarOffset.Bottom;

                Bar.Draw(SBatch, this, BarOffset.Left, BarOffset.Y, barWidth, barHeight);
            }

            /** Draw value label **/
            if (Caption != null && CaptionStyle != null)
            {
                if (!UISpriteBatch.Invalidated)
                {
                    var displayPercent = Math.Round(percent * 100);
                    this.DrawLocalString(SBatch, string.Format(Caption, displayPercent), Vector2.Zero, CaptionStyle, m_Bounds, TextAlignment.Center | TextAlignment.Middle);
                }
            }
        }
    }
}
