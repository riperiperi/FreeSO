/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
Mats 'Afr0' Vederhus. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NAudio.Wave;
using Un4seen.Bass;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.Utils;
using TSOClient.Code.UI.Framework.Parser;
using TSOClient.Code.UI.Model;
using TSOClient.Code;

namespace TSOClient.LUI
{
    public delegate void ButtonClickDelegate(UIElement button);

    /// <summary>
    /// A drawable, clickable button that is part of the GUI.
    /// </summary>
    public class UIButton : UIElement
    {
        public static Texture2D StandardButton;

        static UIButton()
        {
            StandardButton = UIElement.GetTexture((ulong)FileIDs.UIFileIDs.buttontiledialog);
        }

        private int m_CurrentFrame;
        private Texture2D m_Texture;

        private TextStyle m_CaptionStyle = TextStyle.DefaultButton;
        private string m_Caption;

        private Rectangle m_Bounds;
        private int m_Width;
        private int m_WidthDiv3;
        private bool m_Disabled;
        private bool m_HighlightNextDraw;
        private float m_ResizeWidth;

        private UIElementState m_State = UIElementState.Normal;

        public bool Selected { get; set; }

        public UIButton()
            : this(StandardButton)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Texture"></param>
        public UIButton(Texture2D Texture)
        {
            this.Texture = Texture;

            ClickHandler =
                ListenForMouse(new Rectangle(0, 0, m_Width, m_Texture.Height), new UIMouseEvent(OnMouseEvent));
        }

        [UIAttribute("size")]
        public Vector2 Size
        {
            get
            {
                return new Vector2(m_WidthDiv3, m_Texture.Height);
            }
            set
            {
                Width = value.X;
            }
        }

        public float Width
        {
            get { return m_ResizeWidth; }
            set
            {
                m_ResizeWidth = value;
                if (ClickHandler != null)
                {
                    ClickHandler.Region.Width = (int)value;
                }
                m_Bounds = Rectangle.Empty;
            }
        }

        [UIAttribute("text", DataType=UIAttributeType.StringTable)]
        public string Caption
        {

            get { return m_Caption; }
            set {
                m_Caption = value;
                m_CalcAutoSize = true;
            }
        }

        [UIAttribute("font", typeof(TextStyle))]
        public TextStyle CaptionStyle
        {
            get
            {
                return m_CaptionStyle;
            }
            set
            {
                m_CaptionStyle = value;
                m_CalcAutoSize = true;
            }
        }

        private UIMouseEventRef ClickHandler;

        [UIAttribute("image")]
        public Texture2D Texture 
        {
            get { return m_Texture; }
            set {
                m_Texture = value;
                m_Bounds = Rectangle.Empty;

                m_Width = m_Texture.Width / 4;
                m_WidthDiv3 = m_Width / 3;
                m_CurrentFrame = 0;

                if (ClickHandler != null)
                {
                    ClickHandler.Region.Width = (m_ResizeWidth == 0) ? m_Width : (int)m_ResizeWidth;
                    ClickHandler.Region.Height = m_Texture.Height;
                }
            } 
        }

        private bool m_CalcAutoSize;
        private void CalculateAutoSize()
        {
            m_CalcAutoSize = false;
            if (m_ResizeWidth == 0)
            {
                /** Measure the text **/
                var size = m_CaptionStyle.SpriteFont.MeasureString(m_Caption);
                size.X *= m_CaptionStyle.Scale;
                size.Y *= m_CaptionStyle.Scale;

                Width = (m_WidthDiv3 * 2) + size.X;
            }
        }

        private void CalculateState() {
            if (m_Disabled) { 
                m_State = UIElementState.Disabled;
                return;
            }

            m_State = UIElementState.Normal;
            switch(m_CurrentFrame){
                case 1:
                    m_State = UIElementState.Selected;
                    break;

                case 2:
                    m_State = UIElementState.Highlighted;
                    break;
            }
            
        }

        private bool m_Clicking = false;

        public event ButtonClickDelegate OnButtonClick;

        public bool Highlight
        {
            set { m_HighlightNextDraw = value; }
        }

        public bool Disabled
        {
            get { return m_Disabled; }
            set { m_Disabled = value; CalculateState(); }
        }

        /// <summary>
        /// Gets or sets the current frame for this button.
        /// </summary>
        public int CurrentFrame
        {
            get
            {
                if (m_CurrentFrame == 0)
                    return 0;
                else
                    return m_Texture.Width / 4 * m_CurrentFrame;
            }

            set
            {
                //Frames go from 0 to 3.
                if(value < 4)
                {
                    m_CurrentFrame = value;
                    CalculateState();
                }
            }
        }

        private bool m_isOver;
        private bool m_isDown;

        private void OnMouseEvent(UIMouseEventType type, UpdateState state)
        {
            if (m_Disabled) { return; }

            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    m_isOver = true;
                    if (!m_isDown)
                    {
                        m_CurrentFrame = 2;
                    }
                    break;

                case UIMouseEventType.MouseOut:
                    m_isOver = false;
                    if (!m_isDown)
                    {
                        m_CurrentFrame = 0;
                    }
                    break;

                case UIMouseEventType.MouseDown:
                    m_isDown = true;
                    m_CurrentFrame = 1;
                    break;

                case UIMouseEventType.MouseUp:
                    if (m_isDown)
                    {
                        if (OnButtonClick != null)
                        {
                            OnButtonClick(this);
                            GameFacade.SoundManager.PlayUISound(1);
                        }
                    }
                    m_isDown = false;
                    m_CurrentFrame = m_isOver ? 2 : 0;
                    break;
            }

            CalculateState();
        }

        public override void Draw(UISpriteBatch SBatch)
        {
            if (!Visible) { return; }

            if (m_CalcAutoSize)
            {
                CalculateAutoSize();
            }

            /** Draw the button as a 3 slice **/
            var frame = m_CurrentFrame;
            if (Selected)
            {
                frame = 1;
            }
            if (m_Disabled)
            {
                frame = 3;
            }
            int offset = frame * m_Width;


            if (Width != 0)
            {
                //TODO: Work out these numbers once & cache them. Invalidate when texture or width changes

                /** left **/
                base.DrawLocalTexture(SBatch, m_Texture, new Rectangle(offset, 0, m_WidthDiv3, m_Texture.Height), Vector2.Zero);

                /** center **/
                base.DrawLocalTexture(SBatch, m_Texture, new Rectangle(offset + m_WidthDiv3, 0, m_WidthDiv3, m_Texture.Height), new Vector2(m_WidthDiv3, 0), new Vector2( (Width - (m_WidthDiv3 * 2)) / m_WidthDiv3, 1.0f));

                /** right **/
                base.DrawLocalTexture(SBatch, m_Texture, new Rectangle(offset + (m_Width - m_WidthDiv3), 0, m_WidthDiv3, m_Texture.Height), new Vector2(Width - m_WidthDiv3, 0));
            }
            else
            {
                base.DrawLocalTexture(SBatch, m_Texture, new Rectangle(offset, 0, m_Width, m_Texture.Height), Vector2.Zero);
            }

            /**
             * Label
             */
            if (m_Caption != null && m_CaptionStyle != null)
            {
                var box =GetBounds();
                //Little hack to get slightly better centering on text on buttons
                box.Height -= 2;
                this.DrawLocalString(SBatch, m_Caption, Vector2.Zero, m_CaptionStyle, box, TextAlignment.Center | TextAlignment.Middle, Rectangle.Empty, m_State);
            }
        }

        public override Rectangle GetBounds()
        {
            if (m_Bounds == Rectangle.Empty)
            {
                if (Width != 0)
                {
                    m_Bounds = new Rectangle(0, 0, (int)Width, m_Texture.Height);
                }
                else
                {
                    m_Bounds = new Rectangle(0, 0, m_WidthDiv3, m_Texture.Height);
                }
            }
            return m_Bounds;
        }
    }
}
