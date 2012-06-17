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

namespace TSOClient.LUI
{
    class TextLine
    {
        public StringBuilder Content;
        public int YPosition;
        private bool m_Display;

        public TextLine(StringBuilder SB, int YPos)
        {
            Content = SB;
            YPosition = YPos;
            m_Display = true;
        }

        public bool Display
        {
            get { return m_Display; }
            set { m_Display = value; }
        }
    }

    /// <summary>
    /// A drawable, multiline textedit component for the UI.
    /// Currently only draws text, has no background.
    /// </summary>
    public class UITextEdit : UIElement
    {
        private int m_X, m_Y, m_Width, m_Height;
        private int m_Capacity = 50;
        private bool m_HasFocus = false;

        private KeyboardState m_CurrentKeyState;
        private KeyboardState m_OldKeyState;

        private List<TextLine> m_Lines = new List<TextLine>();
        private int m_LinePositionCounter = 0;
        private bool m_TextStartedScrolling = false;

        private bool m_ReadOnly = false;

        public bool HasFocus
        {
            get { return m_HasFocus; }
            set { m_HasFocus = value; }
        }

        /// <summary>
        /// Returns the textual content of this UITextEdit instance.
        /// </summary>
        public string Content
        {
            get
            {
                string Str = "";

                foreach (TextLine Line in m_Lines)
                    Str += Line.Content;

                return Str;
            }
        }

        /// <summary>
        /// Counter that helps with drawing lines.
        /// If there is 1 textline, this property returns 0.
        /// Else it returns 5 for 2 lines and returns 5 more
        /// for every line after that.
        /// </summary>
        public int LineCounter
        {
            get
            {
                if (m_Lines.Count == 1)
                    return 0;
                else
                    return m_LinePositionCounter;
            }
        }

        public int X
        {
            get { return m_X; }
        }

        public int Y
        {
            get { return m_Y; }
        }

        public int Width
        {
            get { return m_Width; }
        }

        public int Height
        {
            get { return m_Height; }
        }

        /// <summary>
        /// Constructs a new UITextEdit instance.
        /// </summary>
        /// <param name="X">The X position of where this UITextEdit is supposed to be visible on screen.</param>
        /// <param name="Y">The Y position of where this UITextEdit is supposed to be visible on screen.</param>
        /// <param name="Width">The Width of this UITextEdit control.</param>
        /// <param name="Height">The Height of this UITextEdit control.</param>
        /// <param name="ReadOnly">Can the user edit the text in this control?</param>
        /// <param name="Capacity">The capacity of this UITextEdit instance, in number of characters.</param>
        /// <param name="StrID">The String ID of this UITextEdit instance.</param>
        /// <param name="Screen">A UIScreen instance.</param>
        public UITextEdit(int X, int Y, int Width, int Height, bool ReadOnly, int Capacity, string StrID, UIScreen Screen)
            : base(Screen, StrID, DrawLevel.DontGiveAFuck)
        {
            m_X = X;
            m_Y = Y;
            m_Width = Width;
            m_Height = Height;
            m_ReadOnly = ReadOnly;
            m_Capacity = Capacity;

            m_Lines.Add(new TextLine(new StringBuilder(), Y));
            m_LinePositionCounter = Y;
        }

        public override void Update(GameTime GTime, ref MouseState CurrentMouseState, 
            ref MouseState PrevioMouseState)
        {
            if (CurrentMouseState.X >= m_X && CurrentMouseState.X <= (m_X + m_Width) &&
                CurrentMouseState.Y > m_Y && CurrentMouseState.Y < (m_Y + m_Height))
            {
                if (CurrentMouseState.LeftButton == ButtonState.Pressed &&
                    PrevioMouseState.LeftButton == ButtonState.Released)
                {
                    if (!m_HasFocus)
                        m_HasFocus = true;
                }
            }

            //Remove focus if the user clicks anywhere outside the control.
            /*if (CurrentMouseState.LeftButton == ButtonState.Pressed &&
                PrevioMouseState.LeftButton == ButtonState.Released)
            {
                m_HasFocus = false;
            }*/

            //Is this a read-only control?
            if (!m_ReadOnly)
            {
                if (m_HasFocus)
                {
                    m_CurrentKeyState = Keyboard.GetState();
                    Keys[] PressedKeys = m_CurrentKeyState.GetPressedKeys();

                    for (int j = 0; j < PressedKeys.Length; j++)
                    {
                        if (!m_CurrentKeyState.IsKeyUp(PressedKeys[j]) && m_OldKeyState.IsKeyUp(PressedKeys[j]))
                        {
                            if (PressedKeys[j] == Keys.Space)
                                m_Lines[m_Lines.Count - 1].Content.Append(" ");
                            else if (PressedKeys[j] == Keys.OemComma)
                                m_Lines[m_Lines.Count - 1].Content.Append(",");
                            else if (PressedKeys[j] == Keys.OemPeriod)
                                m_Lines[m_Lines.Count - 1].Content.Append(".");
                            else if (PressedKeys[j] == Keys.Enter)
                                m_Lines[m_Lines.Count - 1].Content.Append("\r\n");
                            else if (PressedKeys[j] == Keys.LeftShift)
                                m_Lines[m_Lines.Count - 1].Content.Append("");
                            else if (PressedKeys[j] == Keys.RightShift)
                                m_Lines[m_Lines.Count - 1].Content.Append("");
                            else if (PressedKeys[j] == Keys.NumLock)
                                m_Lines[m_Lines.Count - 1].Content.Append("");
                            else if (PressedKeys[j] == Keys.Back)
                            {
                                m_Lines[m_Lines.Count - 1].Content.Remove(m_Lines[m_Lines.Count - 1].
                                    Content.Length, 1);
                                //TODO: Figure out how to remove a line if all its characters have been deleted...
                            }
                            else
                                m_Lines[m_Lines.Count - 1].Content.Append(PressedKeys[j].ToString());
                        }
                    }

                    //Make sure the text doesn't overflow the width of the control.
                    if (m_Lines[m_Lines.Count - 1].Content.ToString().Length >= (m_Width / 8))
                    {
                        m_LinePositionCounter += 11;
                        TextLine TxtLine = new TextLine(new StringBuilder(), m_LinePositionCounter);

                        m_Lines.Add(TxtLine);
                    }

                    //Make sure the text doesn't overflow the bottom...
                    if (m_Lines[m_Lines.Count - 1].YPosition >= (m_Y + m_Height))
                    {
                        if (m_TextStartedScrolling == false)
                        {
                            m_Lines[0].Display = false;
                            m_TextStartedScrolling = true;
                        }
                        else
                            m_Lines[m_Lines.Count - m_Lines.Count + 1].Display = false;

                        foreach (TextLine Line in m_Lines)
                            Line.YPosition -= 11;
                    }

                    //... or top of the control.
                    foreach (TextLine Line in m_Lines)
                    {
                        if (Line.YPosition < m_Y)
                            Line.Display = false;
                    }

                    m_OldKeyState = m_CurrentKeyState;
                }
            }
        }

        public override void Draw(SpriteBatch SBatch)
        {
            base.Draw(SBatch);

            for (int i = 0; i < m_Lines.Count; i++)
            {
                if (m_Lines[i].Display)
                    SBatch.DrawString(m_Screen.ScreenMgr.SprFontBig, m_Lines[i].Content.ToString(),
                        new Vector2(m_X, m_Lines[i].YPosition), Color.White);
            }
        }

        private bool KeypressTest(Keys theKey)
        {
            if (m_CurrentKeyState.IsKeyUp(theKey) && m_OldKeyState.IsKeyDown(theKey))
                return true;

            return false;
        }
    }
}
