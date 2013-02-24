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

namespace TSOClient.LUI
{
    public delegate void ButtonClickDelegate(UIButton button);

    /// <summary>
    /// A drawable, clickable button that is part of the GUI.
    /// </summary>
    public class UIButton : UIElement
    {
        public static Texture2D StandardButton;


        static UIButton()
        {
            StandardButton = UIElement.GetTexture(0x1e700000001);
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



        private bool m_Clicking = false;

        public event ButtonClickDelegate OnButtonClick;

        public bool Highlight
        {
            set { m_HighlightNextDraw = value; }
        }

        public bool Disabled
        {
            get { return m_Disabled; }
            set { m_Disabled = value; }
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
                    //Bass.BASS_ChannelPlay(UISounds.GetSound(0x01).ThisChannel, false);
                    m_isDown = true;
                    m_CurrentFrame = 1;
                    break;

                case UIMouseEventType.MouseUp:
                    if (m_isDown)
                    {
                        if (OnButtonClick != null)
                        {
                            OnButtonClick(this);
                        }
                    }
                    m_isDown = false;
                    m_CurrentFrame = m_isOver ? 2 : 0;
                    break;
            }
        }

        public override void Draw(SpriteBatch SBatch)
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
                frame = 2;
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
                this.DrawLocalString(SBatch, m_Caption, Vector2.Zero, m_CaptionStyle, GetBounds(), TextAlignment.Center | TextAlignment.Middle);
            }

            //SBatch.DrawString(m_Screen.ScreenMgr.SprFontSmall, m_Caption,
            //                new Vector2(m_X + ((ButtonWidth - CaptionSize.X) / 2.1f) * GlobalScale,
            //                    m_Y + ((ButtonHeight - CaptionSize.Y) / 2) * GlobalScale), Color.Wheat);


            //base.Draw(SBatch);

            //if (!Invisible)
            //{
            //    float GlobalScale = GlobalSettings.Default.ScaleFactor;

            //    if (m_ScaleX == 0 && m_ScaleY == 0)
            //    {
            //        //WARNING: Do NOT refer to m_CurrentFrame, as the accessor ensures the right
            //        //value is returned.
            //        SBatch.Draw(m_Texture, new Vector2(m_X * GlobalScale, m_Y * GlobalScale), new Rectangle(CurrentFrame, 0, m_Width, m_Texture.Height), 
            //            Color.White, 0.0f, new Vector2(0.0f, 0.0f), GlobalScale, SpriteEffects.None, 0.0f);

            //        if (m_Caption != null)
            //        {
            //            Vector2 CaptionSize = m_Screen.ScreenMgr.SprFontSmall.MeasureString(m_Caption);
            //            CaptionSize.X += GlobalScale;
            //            CaptionSize.Y += GlobalScale;
            //            float ButtonWidth = m_Width * GlobalScale;
            //            float ButtonHeight = m_Texture.Height * GlobalScale;

            //            SBatch.DrawString(m_Screen.ScreenMgr.SprFontSmall, m_Caption,
            //                new Vector2(m_X + (((ButtonWidth - CaptionSize.X) / 2.1f) * GlobalScale), 
            //                    m_Y + ((ButtonHeight - CaptionSize.Y) / 2) * GlobalScale), Color.Wheat);
            //        }
            //    }
            //    else
            //    {
            //        //WARNING: Do NOT refer to m_CurrentFrame, as the accessor ensures the right
            //        //value is returned.
            //        SBatch.Draw(m_Texture, new Vector2(m_X * GlobalScale, m_Y * GlobalScale), new Rectangle(CurrentFrame, 0, m_Width, m_Texture.Height),
            //            Color.White, 0.0f, new Vector2(0.0f, 0.0f), new Vector2(m_ScaleX + GlobalScale, m_ScaleY + GlobalScale), 
            //            SpriteEffects.None, 0.0f);

            //        if (m_Caption != null)
            //        {
            //            Vector2 CaptionSize = m_Screen.ScreenMgr.SprFontSmall.MeasureString(m_Caption);
            //            CaptionSize.X += GlobalScale;
            //            CaptionSize.Y += GlobalScale;
            //            float ButtonWidth = m_Width * (GlobalScale + m_ScaleX);
            //            float ButtonHeight = m_Texture.Height * (GlobalScale + m_ScaleY);

            //            SBatch.DrawString(m_Screen.ScreenMgr.SprFontSmall, m_Caption,
            //                new Vector2(m_X + ((ButtonWidth - CaptionSize.X) / 2.1f) * GlobalScale,
            //                    m_Y + ((ButtonHeight - CaptionSize.Y) / 2) * GlobalScale), Color.Wheat);
            //        }
            //    }
            //}
        }




        /*

        public override void Update(GameTime GTime, ref MouseState CurrentMouseState, 
            ref MouseState PrevioMouseState)
        {
            base.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);

            float Scale = GlobalSettings.Default.ScaleFactor;

            if (!Invisible)
            {
                if (!Disabled)
                {
                    if (CurrentMouseState.X >= m_X && CurrentMouseState.X <= ((m_X + ((m_Width * Scale) + m_ScaleX)) * Scale) &&
                        CurrentMouseState.Y > m_Y && CurrentMouseState.Y < ((m_Y + ((m_Texture.Height * Scale) + m_ScaleY) * Scale)))
                    {
                        if (!m_Clicking)
                            CurrentFrame = 2;

                        if (CurrentMouseState.LeftButton == ButtonState.Pressed &&
                            PrevioMouseState.LeftButton == ButtonState.Released)
                        {
                            m_Clicking = true;
                            //Setting this to 1 seems to cause the animation to be somewhat glitchy,
                            //and I haven't been able to figure out why.
                            CurrentFrame = 0;


                            Bass.BASS_ChannelPlay(UISounds.GetSound(0x01).ThisChannel, false);

                            //LuaInterfaceManager.CallFunction("ButtonHandler", this);

                            //This event usually won't be subscribed to,
                            //it is only used by dialogs that creates buttons
                            //and wants to handle them internally.
                            if (OnButtonClick != null)
                                OnButtonClick(this);
                        }
                        else
                            m_Clicking = false;
                    }
                    else
                    {
                        m_Clicking = false;
                        CurrentFrame = 0;
                    }
                    if (m_HighlightNextDraw)
                        CurrentFrame = 1;
                }
                else
                {
                    CurrentFrame = 3;
                }

                m_HighlightNextDraw = false;
            }
        }
        */


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
