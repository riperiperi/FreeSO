﻿/*The contents of this file are subject to the Mozilla Public License Version 1.1
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
    /// <summary>
    /// A drawable, clickable button that is part of the GUI.
    /// This button does not render a texture, only text (so
    /// it is more like a clickable label).
    /// </summary>
    public class UITextButton : UIElement
    {
        public delegate void ButtonClickDelegateWithSender(UIElement sender);
        private float m_X, m_Y;
        private int m_CurrentFrame;
        private string myString;
        private string m_StrID;
        private int m_Width;

        private IWavePlayer m_WaveOutDevice = new DirectSoundOut();

        private bool m_Clicking = false;

        public event ButtonClickDelegateWithSender OnButtonClick;

        /// <summary>
        /// Gets or sets the x-coordinate for where to render this button.
        /// </summary>
        public float X
        {
            get { return m_X; }
            set { m_X = value; }
        }

        /// <summary>
        /// Gets or sets the y-coordinate for where to render this button.
        /// </summary>
        public float Y
        {
            get { return m_Y; }
            set { m_Y = value; }
        }

        public int CurrentFrame
        {
            get
            {
                return m_CurrentFrame;
            }

            set
            {
                //Frames go from 0 to 3.
                if (value < 4)
                {
                    m_CurrentFrame = value;
                }
            }
        }

        public UITextButton(float X, float Y, string Text, string StrID, UIScreen Screen)
            : base(Screen, StrID, DrawLevel.DontGiveAFuck)
        {
            m_X = X;
            m_Y = Y;
            myString = Text;
            m_StrID = StrID;
            //All buttons have 4 frames...
            m_Width = 15;
            m_CurrentFrame = 0;
        }

        public override void Update(GameTime GTime, ref MouseState CurrentMouseState,
            ref MouseState PrevioMouseState)
        {
            base.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);

            float Scale = GlobalSettings.Default.ScaleFactor;

            if (CurrentMouseState.X >= m_X && CurrentMouseState.X <= (m_X + (m_Width * Scale)) &&
                CurrentMouseState.Y > m_Y && CurrentMouseState.Y < (m_Y + 25))
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

                    //This event ususally won't be subscribed to,
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
        }

        public override void Draw(SpriteBatch SBatch)
        {
            base.Draw(SBatch);

            float Scale = GlobalSettings.Default.ScaleFactor;

            //WARNING: Do NOT refer to m_CurrentFrame, as the accessor ensures the right
            //value is returned.
            Color c = Color.White;
            switch (CurrentFrame)
            {
                case 0: c = Color.AliceBlue; break;
                case 1: c = Color.Wheat; break;
                case 2: c = Color.White; break;
                case 3: c = Color.Gray; break;
            }
            SBatch.DrawString(m_Screen.ScreenMgr.SprFontBig, myString, new Vector2(m_X * Scale, m_Y * Scale), c);
        }
    }
}