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
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using SimsLib.FAR3;

namespace TSOClient.LUI
{
    public class UIMessageBox : UIElement
    {
        private FAR3Archive m_Archive;
        //Should this messagebox be displayed? Set to false when clicking on 'OK'...
        private bool m_Display = true;

        private float m_X, m_Y;
        private Texture2D m_DiagBackgrnd;
        //The texture for the corner of the dialog.
        private Texture2D m_DiagCorner;

        private UIButton m_OkBtn;

        private string m_Message;

        //This is true when the left mousebutton is held down to drag the messagebox around.
        private bool m_Drag = false;

        public UIMessageBox(float X, float Y, string Message, UIScreen Screen, string StrID)
            : base(Screen, StrID, DrawLevel.AlwaysOnTop)
        {
            m_Archive = new FAR3Archive(GlobalSettings.Default.StartupPath + "uigraphics\\dialogs\\dialogs.dat");

            m_X = X;
            m_Y = Y;

            MemoryStream TexStream = new MemoryStream(m_Archive.GetItemByID(0xE5));
            m_DiagBackgrnd = Texture2D.FromFile(Screen.ScreenMgr.GraphicsDevice, TexStream);

            TexStream = new MemoryStream(m_Archive.GetItemByID(0x185));
            m_DiagCorner = Texture2D.FromFile(Screen.ScreenMgr.GraphicsDevice, TexStream);

            TexStream = new MemoryStream(m_Archive.GetItemByID(0x892));
            Texture2D BtnTex = Texture2D.FromFile(Screen.ScreenMgr.GraphicsDevice, TexStream);
            ManualTextureMask(ref BtnTex, new Color(255, 0, 255));

            float Scale = GlobalSettings.Default.ScaleFactor;

            m_OkBtn = new UIButton(((m_X + (m_DiagBackgrnd.Width * Scale) + 94) - (m_DiagCorner.Width * Scale)) * Scale,
                ((m_Y + (m_DiagBackgrnd.Height * Scale) + 37) - (m_DiagCorner.Height * Scale)) * Scale, .15f, .4f, BtnTex, "BtnOK", Screen);
            m_OkBtn.OnButtonClick += new ButtonClickDelegate(m_OkBtn_OnButtonClick);

            m_Message = Message;
        }

        void m_OkBtn_OnButtonClick(UIButton btn)
        {
            m_Display = false;
        }

        public override void Update(GameTime GTime, ref MouseState CurrentMouseState, 
            ref MouseState PrevioMouseState)
        {
            base.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);

            float Scale = GlobalSettings.Default.ScaleFactor;

            if (CurrentMouseState.X >= m_X && CurrentMouseState.X <= (m_X + ((m_DiagBackgrnd.Width * Scale) + 94)) &&
                CurrentMouseState.Y > m_Y && CurrentMouseState.Y < (m_Y + ((m_DiagBackgrnd.Height * Scale) + 37)))
            {
                if (CurrentMouseState.LeftButton == ButtonState.Pressed && 
                    PrevioMouseState.LeftButton == ButtonState.Pressed)
                {
                    //TODO: Center the control based on the position of the mousecursor.
                    m_Drag = true;
                    m_X = CurrentMouseState.X;
                    m_Y = CurrentMouseState.Y;
                }
            }

            if (CurrentMouseState.LeftButton == ButtonState.Pressed &&
                PrevioMouseState.LeftButton == ButtonState.Pressed)
            {
                if (m_Drag)
                {
                    //TODO: Center the control based on the position of the mousecursor.
                    m_X = CurrentMouseState.X;
                    m_Y = CurrentMouseState.Y;
                    m_OkBtn.X = ((m_X + (m_DiagBackgrnd.Width * Scale) + 94) - (m_DiagCorner.Width * Scale)) * Scale;
                    m_OkBtn.Y = ((m_Y + (m_DiagBackgrnd.Height * Scale) + 37) - (m_DiagCorner.Height * Scale)) * Scale;
                }
            }
            else if (CurrentMouseState.LeftButton == ButtonState.Released &&
                PrevioMouseState.LeftButton == ButtonState.Pressed)
            {
                m_Drag = false;
            }

            m_OkBtn.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);
        }

        public override void Draw(SpriteBatch SBatch)
        {
            base.Draw(SBatch);

            if (m_Display)
            {
                float Scale = GlobalSettings.Default.ScaleFactor;

                SBatch.Draw(m_DiagBackgrnd, new Vector2(m_X * Scale, m_Y * Scale), null, Color.White, 0.0f,
                    new Vector2(0.0f, 0.0f), new Vector2(Scale + .50f, Scale + .25f), SpriteEffects.None, 0.0f); 

                SBatch.Draw(m_DiagCorner, new Vector2((m_X + ((m_DiagBackgrnd.Width + 94) * Scale) -
                    ((m_DiagCorner.Width + 11) * Scale)) * Scale, (m_Y + ((m_DiagBackgrnd.Height * Scale) + 34) -
                    (m_DiagCorner.Height * Scale) * Scale)), null, Color.White, 0.0f, new Vector2(0.0f, 0.0f),
                    new Vector2(Scale + .12f, Scale + .3f), SpriteEffects.None, 0.0f);

                m_OkBtn.Draw(SBatch);

                SBatch.DrawString(m_Screen.ScreenMgr.SprFontBig, m_Message, new Vector2((m_X + 40),
                    (m_Y + 50)), Color.Wheat);
            }
        }
    }
}
