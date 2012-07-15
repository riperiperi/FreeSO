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

namespace TSOClient.LUI
{
    public delegate void ButtonClickDelegate(UIButton button);

    /// <summary>
    /// A drawable, clickable button that is part of the GUI.
    /// </summary>
    public class UIButton : UIElement
    {
        private int m_X, m_Y, m_ScaleX, m_ScaleY, m_CurrentFrame;
        private Texture2D m_Texture;
        private string m_Caption, m_StrID;
        private int m_Width;
        private bool m_Disabled;
        private bool m_HighlightNextDraw;
        private bool m_Invisible;

        public bool Invisible { set { m_Invisible = value; } get { return m_Invisible; } }

        public Texture2D Texture { set { m_Texture = value; } }

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
        /// Gets or sets the x-coordinate for where to render this button.
        /// </summary>
        public int X
        {
            get { return m_X; }
            set { m_X = value; }
        }

        /// <summary>
        /// Gets or sets the y-coordinate for where to render this button.
        /// </summary>
        public int Y
        {
            get { return m_Y; }
            set { m_Y = value; }
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

        /// <summary>
        /// Gets or sets the scalingfactor for this button
        /// on the X-axis.
        /// </summary>
        public int ScaleX
        {
            get { return m_ScaleX; }
            set { m_ScaleX = value; }
        }

        /// <summary>
        /// Gets or sets the scalingfactor for this button
        /// on the Y-axis.
        /// </summary>
        public int ScaleY
        {
            get { return m_ScaleY; }
            set { m_ScaleY = value; }
        }

        #region Constructors

        /// <summary>
        /// Creates an instance of UIButton that has a texture.
        /// </summary>
        /// <param name="X">The x-coordinate where the button will be displayed.</param>
        /// <param name="Y">The y-coordinate where the button will be displayed.</param>
        /// <param name="Texture">The texture for this button.</param>
        /// <param name="Enabled">Is this button enabled?</param>
        /// <param name="StrID">The button's string ID.</param>
        /// <param name="Screen">The UIScreen instance that will draw and update this button.</param>
        public UIButton (int X, int Y, Texture2D Texture, bool Disabled, string StrID, UIScreen Screen)
            : base(Screen, StrID, DrawLevel.DontGiveAFuck)
        {
            m_X = X;
            m_Y = Y;
            m_Texture = Texture;
            m_Disabled = Disabled;
            m_StrID = StrID;
            //All buttons have 4 frames...
            m_Width = Texture.Width / 4;
            m_CurrentFrame = 0;
            OnButtonClick += new ButtonClickDelegate(delegate(UIButton btn) { Screen.RegisterClick(this); });
        }

        /// <summary>
        /// Creates an instance of UIButton that has a texture and a caption.
        /// </summary>
        /// <param name="X">The x-coordinate where the button will be displayed.</param>
        /// <param name="Y">The y-coordinate where the button will be displayed.</param>
        /// <param name="Texture">The texture for this button.</param>
        /// <param name="Caption">The button's caption.</param>
        /// <param name="CaptionID">The ID for the string to use as the button's caption.</param>
        /// <param name="Screen">The UIScreen instance that will draw and update this button.</param>
        public UIButton(int X, int Y, Texture2D Texture, int CaptionID, string StrID, UIScreen Screen)
            : base(Screen, StrID, DrawLevel.DontGiveAFuck)
        {
            m_X = X;
            m_Y = Y;
            m_Texture = Texture;
            m_StrID = StrID;
            //All buttons have 4 frames...
            m_Width = Texture.Width / 4;
            m_CurrentFrame = 0;
            OnButtonClick += new ButtonClickDelegate(delegate(UIButton btn) { Screen.RegisterClick(this); });

            if (Screen.ScreenMgr.TextDict.ContainsKey(CaptionID))
                m_Caption = Screen.ScreenMgr.TextDict[CaptionID];
        }

        /// <summary>
        /// Creates an instance of UIButton that has a scaled texture.
        /// </summary>
        /// <param name="X">The x-coordinate where the button will be displayed.</param>
        /// <param name="Y">The y-coordinate where the button will be displayed.</param>
        /// <param name="ScaleX">The scaling-factor used to scale this button's texture on the X-axis.</param>
        /// <param name="ScaleY">The scaling-factor used to scale this button's texture on the Y-axis.</param>
        /// <param name="Texture">The texture for this button.</param>
        /// <param name="StrID">The button's string ID.</param>
        /// <param name="Screen">The UIScreen instance that will draw and update this button.</param>
        public UIButton(int X, int Y, int ScaleX, int ScaleY, Texture2D Texture, string StrID, UIScreen Screen)
            : base(Screen, StrID, DrawLevel.DontGiveAFuck)
        {
            m_X = X;
            m_Y = Y;
            m_ScaleX = ScaleX;
            m_ScaleY = ScaleY;
            m_Texture = Texture;
            m_StrID = StrID;
            //All buttons have 4 frames...
            m_Width = Texture.Width / 4;
            m_CurrentFrame = 0;
            OnButtonClick += new ButtonClickDelegate(delegate(UIButton btn) { Screen.RegisterClick(this); });
        }

        /// <summary>
        /// Creates an instance of UIButton that has a scaled texture and a caption.
        /// </summary>
        /// <param name="X">The x-coordinate where the button will be displayed.</param>
        /// <param name="Y">The y-coordinate where the button will be displayed.</param>
        /// <param name="ScaleX">The scaling-factor used to scale this button's texture on the X-axis.</param>
        /// <param name="ScaleY">The scaling-factor used to scale this button's texture on the Y-axis.</param>
        /// <param name="Texture">The texture for this button.</param>
        /// <param name="CaptionID">The ID for the string to use as the button's caption.</param>
        /// <param name="StrID">The button's string ID.</param>
        /// <param name="Screen">The UIScreen instance that will draw and update this button.</param>
        public UIButton(int X, int Y, int ScaleX, int ScaleY, Texture2D Texture, int CaptionID, 
            string StrID, UIScreen Screen) : base(Screen, StrID, DrawLevel.DontGiveAFuck)
        {
            m_X = X;
            m_Y = Y;
            m_ScaleX = ScaleX;
            m_ScaleY = ScaleY;
            m_Texture = Texture;
            m_StrID = StrID;
            //All buttons have 4 frames...
            m_Width = Texture.Width / 4;
            m_CurrentFrame = 0;
            OnButtonClick += new ButtonClickDelegate(delegate(UIButton btn) { Screen.RegisterClick(this); });

            if (Screen.ScreenMgr.TextDict.ContainsKey(CaptionID))
                m_Caption = Screen.ScreenMgr.TextDict[CaptionID];
        }

        #endregion

        public override void Update(GameTime GTime, ref MouseState CurrentMouseState, 
            ref MouseState PrevioMouseState)
        {
            base.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);

            if (!Invisible)
            {
                if (!Disabled)
                {
                    if (CurrentMouseState.X >= m_X && CurrentMouseState.X <= (m_X + (m_Width + m_ScaleX)) &&
                        CurrentMouseState.Y > m_Y && CurrentMouseState.Y < (m_Y + (m_Texture.Height + m_ScaleY)))
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

        public void BtnHandle()
        {
            LuaInterfaceManager.CallFunction("ButtonHandler", this);
        }

        public override void Draw(SpriteBatch SBatch)
        {
            base.Draw(SBatch);

            if (!Invisible)
            {
                int GlobalScale = GlobalSettings.Default.ScaleFactor;

                if (m_ScaleX == 0 && m_ScaleY == 0)
                {
                    //WARNING: Do NOT refer to m_CurrentFrame, as the accessor ensures the right
                    //value is returned.
                    SBatch.Draw(m_Texture, new Vector2(m_X, m_Y),
                        new Rectangle(CurrentFrame, 0, m_Width * GlobalScale, m_Texture.Height * GlobalScale), Color.White);

                    if (m_Caption != null)
                    {
                        Vector2 CaptionSize = m_Screen.ScreenMgr.SprFontSmall.MeasureString(m_Caption);

                        SBatch.DrawString(m_Screen.ScreenMgr.SprFontSmall, m_Caption,
                            new Vector2(m_X + ((m_Width - CaptionSize.X) / 2), 
                                m_Y + ((m_Texture.Height - CaptionSize.Y) / 2)), Color.Wheat);
                    }
                }
                else
                {
                    //WARNING: Do NOT refer to m_CurrentFrame, as the accessor ensures the right
                    //value is returned.
                    SBatch.Draw(m_Texture, new Rectangle(m_X, m_Y, (m_Width + m_ScaleX) * GlobalScale, m_Texture.Height +
                        m_ScaleY * GlobalScale), new Rectangle(CurrentFrame, 0, m_Width, m_Texture.Height), Color.White);

                    if (m_Caption != null)
                    {
                        Vector2 CaptionSize = m_Screen.ScreenMgr.SprFontSmall.MeasureString(m_Caption);
                        int ButtonWidth = m_Width + m_ScaleX;
                        int ButtonHeight = m_Texture.Height + m_ScaleY;

                        SBatch.DrawString(m_Screen.ScreenMgr.SprFontSmall, m_Caption,
                            new Vector2(m_X + ((ButtonWidth - CaptionSize.X) / 2),
                                m_Y + ((ButtonHeight - CaptionSize.Y) / 2)), Color.Wheat);
                    }
                }
            }
        }
    }
}
