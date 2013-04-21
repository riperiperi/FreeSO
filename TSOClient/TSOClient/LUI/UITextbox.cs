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
using LogThis;

namespace TSOClient.LUI
{
    class DrawableCharacter
    {
        private string m_Char;
        private float m_X = 0.0f;
        bool m_Visible = false;

        public string Character
        {
            get { return m_Char; }
            set { m_Char = value; }
        }

        public float X
        {
            get { return m_X; }
            set { m_X = value; }
        }

        public bool Visible
        {
            get { return m_Visible; }
            set { m_Visible = value; }
        }

        public DrawableCharacter(string Character, float X, bool Visible)
        {
            m_Char = Character;
            m_X = X;
            m_Visible = Visible;
        }
    }

    /// <summary>
    /// A single-line textbox element, used for making the user input text.
    /// </summary>
    public class UITextbox : UIElement
    {
        private StringBuilder m_SBuilder = new StringBuilder();
        //For loading resources.
        private FAR3Archive m_Archive;
        //The background texture for this textbox. Must be drawn multiple times (as tiles).
        private Texture2D m_BackgroundTex;
        private int m_Transparency = 255; //Defaults to no transparency at all.

        private float m_X, m_Y, m_CursorDrawX;
        //How many tiles of m_BackgroundTex will have to be drawn to get the correct width of the textbox.
        private int m_NumTiles = 0;
        private int m_Width = 0, m_Height = 0;

        //Does this TextBox currently have focus?
        private bool m_HasFocus = false;
        private KeyboardState m_CurrentKeyState;
        private KeyboardState m_OldKeyState;

        /// <summary>
        /// Returns the current text (input) in this textbox.
        /// </summary>
        public string CurrentText
        {
            get { return m_SBuilder.ToString(); }
        }

        /// <summary>
        /// Constructor for the UITextbox class.
        /// </summary>
        /// <param name="BackgrdID">The ID of the background's texture.</param>
        /// <param name="X">The x-coordinate on the screen of where to draw the textbox.</param>
        /// <param name="Y">The y-coordinate on the screen of where to draw the textbox.</param>
        /// <param name="Width">The width of the textbox in pixels. Should be greater than 39.</param>
        /// <param name="DrawTransparent">Whether or not to draw this textbox transparently.</param>
        /// <param name="Screen">A UIScreen instance.</param>
        /// <param name="StrID">The the string ID of this textbox.</param>
        public UITextbox(uint BackgrdID, float X, float Y, int Width, int Transparency, 
            UIScreen Screen, string StrID) : base(Screen, StrID, DrawLevel.DontGiveAFuck)
        {
            m_Archive = new FAR3Archive(GlobalSettings.Default.StartupPath + "uigraphics\\dialogs\\dialogs.dat");

            MemoryStream TexStream = new MemoryStream(m_Archive.GetItemByID(BackgrdID));
            m_BackgroundTex = Texture2D.FromFile(Screen.ScreenMgr.GraphicsDevice, TexStream);

            m_Transparency = Transparency;

            m_X = X;
            m_Y = Y;
            m_CursorDrawX = (m_X + 3);

            //The Width parameter will hopefully always be greater than 39, as the
            //'dialog_textboxbackground.bmp' image is 39x39 pixels.
            if (Width > m_BackgroundTex.Width)
            {
                m_NumTiles = (int)(Width / (m_BackgroundTex.Width + 3));
                //The background consist of one half tile + one third of a tile as many times as m_NumTiles,
                //then another half tile. Therefore the total width equals the calculation below.
                m_Width = (m_NumTiles * 13) + m_BackgroundTex.Width;
                m_Height = m_BackgroundTex.Height;
            }
            else
            {
                //Originally I set m_NumTiles to m_BackgroundTex.Width here, but realized
                //that that would cause fucking ENDLESS textboxes (39 tiles!!)
                m_NumTiles = Width;
                m_Width = m_BackgroundTex.Width;
                m_Height = m_BackgroundTex.Height;
            }
        }

        public override void Update(GameTime GTime, ref MouseState CurrentMouseState,
            ref MouseState PrevioMouseState)
        {
            base.Update(GTime, ref CurrentMouseState, ref PrevioMouseState);

            float Scale = GlobalSettings.Default.ScaleFactor;

            if (CurrentMouseState.X >= m_X && CurrentMouseState.X <= (m_X + (m_Width * Scale))
                && CurrentMouseState.Y > m_Y && CurrentMouseState.Y < (m_Y + (m_Height * Scale)))
            {
                if (CurrentMouseState.LeftButton == ButtonState.Pressed &&
                    PrevioMouseState.LeftButton == ButtonState.Released)
                {
                    if (!m_HasFocus)
                        m_HasFocus = true;
                }
            }
            else
            {
                //Remove focus if the user clicked elsewhere...
                if (CurrentMouseState.LeftButton == ButtonState.Pressed &&
                    PrevioMouseState.LeftButton == ButtonState.Released)
                {
                    m_HasFocus = false;
                }
            }

            if (m_HasFocus)
            {
                m_CurrentKeyState = Keyboard.GetState();
                Keys[] PressedKeys = m_CurrentKeyState.GetPressedKeys();

                for (int j = 0; j < PressedKeys.Length; j++)
                {
                    if (!m_CurrentKeyState.IsKeyUp(PressedKeys[j]) && m_OldKeyState.IsKeyUp(PressedKeys[j]))
                    {
                        if (PressedKeys[j] == Keys.Space)
                            m_SBuilder.Append(" ");
                        else if (PressedKeys[j] == Keys.OemComma)
                            m_SBuilder.Append(",");
                        else if (PressedKeys[j] == Keys.OemPeriod)
                            m_SBuilder.Append(".");
                        else if (PressedKeys[j] == Keys.Enter)
                            m_SBuilder.Append(""); //Append nothing, multiple lines aren't supported!
                        else if (PressedKeys[j] == Keys.OemTilde)
                            m_SBuilder.Append("ø"); //Might have to change this based on language settings.
                        else if (PressedKeys[j] == Keys.D0)
                            m_SBuilder.Append("0");
                        else if (PressedKeys[j] == Keys.D1)
                        {
                            m_SBuilder.Append("1");
                            m_CursorDrawX += 8;
                        }
                        else if (PressedKeys[j] == Keys.D2)
                        {
                            m_SBuilder.Append("2");
                            m_CursorDrawX += 8;
                        }
                        else if (PressedKeys[j] == Keys.D3)
                        {
                            m_SBuilder.Append("3");
                            m_CursorDrawX += 8;
                        }
                        else if (PressedKeys[j] == Keys.D4)
                        {
                            m_SBuilder.Append("4");
                            m_CursorDrawX += 8;
                        }
                        else if (PressedKeys[j] == Keys.D5)
                        {
                            m_SBuilder.Append("5");
                            m_CursorDrawX += 8;
                        }
                        else if (PressedKeys[j] == Keys.D6)
                        {
                            m_SBuilder.Append("6");
                            m_CursorDrawX += 8;
                        }
                        else if (PressedKeys[j] == Keys.D7)
                        {
                            m_SBuilder.Append("7");
                            m_CursorDrawX += 8;
                        }
                        else if (PressedKeys[j] == Keys.D8)
                        {
                            m_SBuilder.Append("8");
                            m_CursorDrawX += 8;
                        }
                        else if (PressedKeys[j] == Keys.D9)
                        {
                            m_SBuilder.Append("9");
                            m_CursorDrawX += 8;
                        }
                        else if (PressedKeys[j] == Keys.Back)
                        {
                            if (m_SBuilder.Length > 0)
                            {
                                m_SBuilder.Remove((m_SBuilder.Length - 1), 1);

                                //The text area of the control ends roughly 26px before the width...
                                if (m_X + m_Screen.ScreenMgr.SprFontSmall.MeasureString(m_SBuilder.ToString()).X
                                    <= ((m_X + m_Width) - 26))
                                {
                                    if (m_X + m_Screen.ScreenMgr.SprFontSmall.MeasureString(
                                        m_SBuilder.ToString()).X >= (m_X + 3))
                                    {
                                        //Make sure the cursor isn't ever drawn before the end of the text (string)
                                        //when deleting characters.
                                        if (m_CursorDrawX >= (m_X +
                                            m_Screen.ScreenMgr.SprFontSmall.MeasureString(m_SBuilder.ToString()).X + 8))
                                        {
                                            m_CursorDrawX -= 8;
                                        }
                                    }
                                }
                            }
                        }
                        else if (PressedKeys[j] == Keys.None)
                            m_SBuilder.Append("");
                        else if (PressedKeys[j] == Keys.RightShift)
                            m_SBuilder.Append("");
                        else if (PressedKeys[j] == Keys.LeftShift)
                            m_SBuilder.Append("");
                        else if (PressedKeys[j] == Keys.NumLock)
                            m_SBuilder.Append("");
                        else
                        {
                            m_SBuilder.Append(PressedKeys[j].ToString());

                            if (m_CursorDrawX < ((m_X + m_Width) - 23))
                            {
                                //Make sure the cursor's position doesn't continiue on past the end of the text (string)
                                //when typing.
                                if (m_CursorDrawX <= (m_X +
                                    m_Screen.ScreenMgr.SprFontSmall.MeasureString(m_SBuilder.ToString()).X + 4))
                                {
                                    m_CursorDrawX += 8;
                                }
                            }
                        }
                    }
                }

                m_OldKeyState = m_CurrentKeyState;
            }
        }

        private string ClipTextLeft(SpriteFont font, string text, float Width, int pixelBuffer)
        {
            int charIndex = 0;
            string textToWrite = text.Substring(0, text.Length);

            Vector2 fontDimensions = font.MeasureString(textToWrite);

            while (fontDimensions.X > Width - (pixelBuffer * 2))
            {
                charIndex++;
                textToWrite = text.Substring(charIndex, text.Length - charIndex);
                fontDimensions = font.MeasureString(textToWrite);
            }

            return textToWrite;
        }

        private string ClipTextRight(SpriteFont font, string text, int Width, int pixelBuffer)
        {
            int charIndex = text.Length;
            string textToWrite = text.Substring(0, charIndex);

            Vector2 fontDimensions = font.MeasureString(textToWrite);

            while (fontDimensions.X > Width - (pixelBuffer * 2))
            {
                charIndex--;
                textToWrite = text.Substring(0, charIndex);
                fontDimensions = font.MeasureString(textToWrite);
            }

            return textToWrite;
        }

        public override void Draw(SpriteBatch SBatch)
        {
            base.Draw(SBatch);

            float Scale = GlobalSettings.Default.ScaleFactor;

            //First, draw one half tile for the beginning of the background...
            SBatch.Draw(m_BackgroundTex, new Vector2(m_X * Scale, m_Y * Scale), null, Color.White, 0.0f,
                new Vector2(0.0f, 0.0f), Scale, SpriteEffects.None, 0.0f);

            //... then tile as many times as m_NumTiles specifies...
            float X = (m_X + 13);
            for (int i = 0; i < (m_NumTiles - 2); i++)
            {
                X = X + 13;
                SBatch.Draw(m_BackgroundTex, new Vector2(X * Scale, m_Y * Scale), new Rectangle(13, 0, 13, m_BackgroundTex.Height), 
                    new Color(255, 255, 255, m_Transparency), 0.0f, new Vector2(0.0f, 0.0f), Scale, SpriteEffects.None, 0.0f);
            }

            //...and then draw another half tile (the second half this time).
            SBatch.Draw(m_BackgroundTex, new Vector2(X * Scale, m_Y * Scale),
                new Rectangle(19, 0, m_BackgroundTex.Width / 2, m_Height), new Color(255, 255, 255, m_Transparency),
                0.0f, new Vector2(0.0f, 0.0f), Scale, SpriteEffects.None, 0.0f);

            SBatch.DrawString(m_Screen.ScreenMgr.SprFontSmall,
                ClipTextLeft(m_Screen.ScreenMgr.SprFontSmall, m_SBuilder.ToString(), ((m_Width * Scale) - 23), 2),
                new Vector2((m_X + 3) * Scale, (m_Y + 8) * Scale), Color.Wheat);

            if (m_HasFocus)
            {
                SBatch.DrawString(m_Screen.ScreenMgr.SprFontSmall, "|", new Vector2(m_CursorDrawX, (m_Y + 8)),
                    Color.Wheat);
            }
        }
    }
}
