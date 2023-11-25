using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;
using System.Runtime.InteropServices;
using FSO.Common.Rendering.Framework.Model;

namespace FSO.Common.Rendering.Framework.IO
{
    /// <summary>
    /// Manages input for the game.
    /// </summary>
    public class InputManager
    {
        private IFocusableUI LastFocus;
        public bool RequireWindowFocus = false;

        public void SetFocus(IFocusableUI ui)
        {
            /** No change **/
            if (ui == LastFocus) { return; }

            if (LastFocus != null)
            {
                LastFocus.OnFocusChanged(FocusEvent.FocusOut);
            }

            LastFocus = ui;
            if (ui != null)
            {
                LastFocus.OnFocusChanged(FocusEvent.FocusIn);
            }
        }

        public IFocusableUI GetFocus()
        {
            return LastFocus;
        }

        [DllImport("user32.dll")]
        static extern int MapVirtualKey(uint uCode, uint uMapType);

        /// <summary>
        /// Utility to apply the result of pressing keys against a buffer
        /// </summary>
        /// <param name="buffer"></param>
        /// <param name="keys"></param>
        public KeyboardInputResult ApplyKeyboardInput(StringBuilder m_SBuilder, UpdateState state, int cursorIndex, int cursorEndIndex, bool allowInput)
        {
            if (!state.WindowFocused && RequireWindowFocus) { return null; }
            
            var PressedKeys = state.KeyboardState.GetPressedKeys();
            int charCount = 0;
            if (state.FrameTextInput == null) charCount = 0;
            else charCount = state.FrameTextInput.Count;

            if (PressedKeys.Length + charCount == 0) { return null; }
            //bit of a legacy thing going on here
            //we support both "pressed keys" and the keyboard event system.
            //todo: clean up a bit

            var didChange = false;
            var result = new KeyboardInputResult();
            
            var m_CurrentKeyState = state.KeyboardState;
            var m_OldKeyState = state.PreviousKeyboardState;


            result.ShiftDown = PressedKeys.Contains(Keys.LeftShift) || PressedKeys.Contains(Keys.RightShift);
			result.CapsDown = state.KeyboardState.CapsLock;
			result.NumLockDown = state.KeyboardState.NumLock;
            // Right alt aka AltGr is treated as Ctrl+Alt. It is used to type accented letters and other unusual characters so pressing that key cannot cause special actions.
            result.CtrlDown = (PressedKeys.Contains(Keys.LeftControl) && !PressedKeys.Contains(Keys.RightAlt)) || PressedKeys.Contains(Keys.RightControl);

            for (int j = 0; j < state.NewKeys.Count + charCount; j++)
            {
                var key = (j<state.NewKeys.Count)?state.NewKeys[j]:Keys.None;
                bool processChar = true;

                if (key != Keys.None)
                {
                    processChar = false;
                    if (key == Keys.Back || key == Keys.Delete)
                    {
                        if (m_SBuilder.Length > 0)
                        {
                            /**
                             * Delete previous character or delete selection
                             */
                            if (cursorEndIndex == -1 && result.CtrlDown)
                            {
                                /** Delete up until the previous whitespace char **/
                                int newEndIndex = cursorIndex;
                                if (newEndIndex == -1)
                                {
                                    newEndIndex = m_SBuilder.Length;
                                    cursorIndex = m_SBuilder.Length;
                                }
                                int dir = (key == Keys.Delete) ? 1 : -1;
                                int ws = (key == Keys.Delete) ? 0 : -1;
                                while (newEndIndex+ws >= 0 && newEndIndex+ws < m_SBuilder.Length)
                                {
                                    if (Char.IsWhiteSpace(m_SBuilder[newEndIndex+ws]))
                                    {
                                        /** Keep the whitespace char **/
                                        break;
                                    }
                                    newEndIndex += dir;
                                }
                                if (cursorIndex > newEndIndex)
                                {
                                    cursorEndIndex = cursorIndex;
                                    cursorIndex = newEndIndex;
                                }
                                else
                                    cursorEndIndex = newEndIndex;
                                if (cursorEndIndex == cursorIndex)
                                    cursorIndex = cursorEndIndex = -1;
                            }

                            if (cursorEndIndex == -1)
                            {
                                /** Previous character **/
                                var index = cursorIndex == -1 ? m_SBuilder.Length : cursorIndex;
                                if ((key == Keys.Back) && (index > 0))
                                {
                                    var numToDelete = 1;
                                    if (index > 1 && m_SBuilder[index - 1] == '\n' && m_SBuilder[index - 2] == '\r')
                                    {
                                        numToDelete = 2;
                                    }


                                    m_SBuilder.Remove(index - numToDelete, numToDelete);
                                    result.NumDeletes += numToDelete;

                                    if (cursorIndex != -1)
                                    {
                                        cursorIndex -= numToDelete;
                                    }
                                }
                                else if ((key == Keys.Delete) && (index < m_SBuilder.Length))
                                {
                                    /** Guys, delete removes the next character, not the last!! **/
                                    var numToDelete = 1;
                                    if ((index < m_SBuilder.Length - 1) && m_SBuilder[index] == '\r' && m_SBuilder[index + 1] == '\n')
                                    {
                                        numToDelete = 2;
                                    }

                                    m_SBuilder.Remove(index, numToDelete);
                                    result.NumDeletes += numToDelete;
                                }
                            }
                            else
                            {
                                DeleteSelectedText(m_SBuilder, ref cursorIndex, ref cursorEndIndex, ref didChange, result);
                            }
                            result.SelectionChanged = true;
                            didChange = true;
                        }
                    }
                    else if (key == Keys.Enter)
                    {
                        if (allowInput)
                        {
                            /** Line break **/
                            if (cursorEndIndex != -1)
                            {
                                /** Delete selected text **/
                                DeleteSelectedText(m_SBuilder, ref cursorIndex, ref cursorEndIndex, ref didChange, result);
                            }

                            if (cursorIndex == -1)
                            {
                                m_SBuilder.Append("\n");
                            }
                            else
                            {
                                cursorIndex = Math.Min(m_SBuilder.Length, cursorIndex);
                                m_SBuilder.Insert(cursorIndex, "\n");
                                cursorIndex += 1;
                            }
                            result.NumInsertions += 1;
                            didChange = true;
                            result.EnterPressed = true;
                        }
                    }
                    else if (key == Keys.Tab)
                    {
                        result.TabPressed = true;
                    }
                    else if (result.CtrlDown)
                    {
                        switch (key)
                        {
                            case Keys.A:
                                /** Select all **/
                                cursorIndex = 0;
                                cursorEndIndex = m_SBuilder.Length;
                                result.SelectionChanged = true;
                                break;

                            case Keys.C:
                            case Keys.X:
                                /** Copy text to clipboard **/
                                if (cursorEndIndex > 0)
                                {
                                    var selectionStart = Math.Max(0, cursorIndex);
                                    var selectionEnd = cursorEndIndex;
                                    GetSelectionRange(ref selectionStart, ref selectionEnd);

                                    var str = m_SBuilder.ToString().Substring(selectionStart, selectionEnd - selectionStart);

                                    ClipboardHandler.Default.Set(str);

                                    if (key == Keys.X)
                                    {
                                        DeleteSelectedText(m_SBuilder, ref cursorIndex, ref cursorEndIndex, ref didChange, result);
                                    }
                                }
                                break;

                            case Keys.V:
                                /** Paste text in **/
                                var clipboardText = ClipboardHandler.Default.Get();
                                
                                if (clipboardText != null)
                                {
                                    /** TODO: Cleanup the clipboard text to make sure its valid **/

                                    /** If i have selection, delete it **/
                                    if (cursorEndIndex != -1)
                                    {
                                        DeleteSelectedText(m_SBuilder, ref cursorIndex, ref cursorEndIndex, ref didChange, result);
                                    }

                                    /** Paste **/
                                    if (cursorIndex == -1)
                                    {
                                        m_SBuilder.Append(clipboardText);
                                    }
                                    else
                                    {
                                        m_SBuilder.Insert(Math.Min(cursorIndex, m_SBuilder.Length), clipboardText);
                                        cursorIndex += clipboardText.Length;
                                    }
                                    result.NumInsertions += clipboardText.Length;
                                    didChange = true;
                                }
                                break;
                            }
                        } else {
                            result.UnhandledKeys.Add(key);
                            processChar = true;
                        }
                    }

                if (processChar)
                {
                    char value;
                    if (j >= state.NewKeys.Count) value = state.FrameTextInput[j - state.NewKeys.Count];
                    else if (state.FrameTextInput != null) continue;
                    else value = TranslateChar(key, result.ShiftDown, result.CapsDown, result.NumLockDown);
                    /** For now we dont support tabs in text **/
                    
                    if (!char.IsControl(value) && value != '\0' && value != '\t' && value != '\b' && value != '\r')
                    {
                        if (allowInput)
                        {
                            if (cursorEndIndex != -1)
                            {
                                /** Delete selected text **/
                                DeleteSelectedText(m_SBuilder, ref cursorIndex, ref cursorEndIndex, ref didChange, result);
                            }

                            if (cursorIndex == -1)
                            {
                                m_SBuilder.Append(value);
                            }
                            else
                            {
                                m_SBuilder.Insert(cursorIndex, value);
                                cursorIndex++;
                            }
                            result.NumInsertions++;
                            didChange = true;
                        }
                    }
                    else
                    {
                        result.UnhandledKeys.Add(key);
                    }
                }
            }

            result.SelectionStart = cursorIndex;
            result.SelectionEnd = cursorEndIndex;

            result.ContentChanged = didChange;
            return result;
        }

        private void DeleteSelectedText(StringBuilder m_SBuilder, ref int cursorIndex, ref int cursorEndIndex, ref bool didChange, KeyboardInputResult result)
        {
            /** Remove selected text **/
            var index = cursorIndex == -1 ? m_SBuilder.Length : cursorIndex;
            var end = cursorEndIndex;
            if (end < index)
            {
                var temp = index;
                index = end;
                end = temp;
            }
            m_SBuilder.Remove(index, end - index);

            cursorIndex = index;
            if (cursorIndex >= m_SBuilder.Length)
            {
                cursorIndex = -1;
            }
            cursorEndIndex = -1;
            result.SelectionChanged = true;
            didChange = true;
        }

        public void GetSelectionRange(ref int start, ref int end)
        {
            if (end < start)
            {
                var temp = start;
                start = end;
                end = temp;
            }
        }

        public static char TranslateChar(Keys key, bool shift, bool capsLock, bool numLock)
        {

            switch (key)
            {

                case Keys.A: return TranslateAlphabetic('a', shift, capsLock);

                case Keys.B: return TranslateAlphabetic('b', shift, capsLock);

                case Keys.C: return TranslateAlphabetic('c', shift, capsLock);

                case Keys.D: return TranslateAlphabetic('d', shift, capsLock);

                case Keys.E: return TranslateAlphabetic('e', shift, capsLock);

                case Keys.F: return TranslateAlphabetic('f', shift, capsLock);

                case Keys.G: return TranslateAlphabetic('g', shift, capsLock);

                case Keys.H: return TranslateAlphabetic('h', shift, capsLock);

                case Keys.I: return TranslateAlphabetic('i', shift, capsLock);

                case Keys.J: return TranslateAlphabetic('j', shift, capsLock);

                case Keys.K: return TranslateAlphabetic('k', shift, capsLock);

                case Keys.L: return TranslateAlphabetic('l', shift, capsLock);

                case Keys.M: return TranslateAlphabetic('m', shift, capsLock);

                case Keys.N: return TranslateAlphabetic('n', shift, capsLock);

                case Keys.O: return TranslateAlphabetic('o', shift, capsLock);

                case Keys.P: return TranslateAlphabetic('p', shift, capsLock);

                case Keys.Q: return TranslateAlphabetic('q', shift, capsLock);

                case Keys.R: return TranslateAlphabetic('r', shift, capsLock);

                case Keys.S: return TranslateAlphabetic('s', shift, capsLock);

                case Keys.T: return TranslateAlphabetic('t', shift, capsLock);

                case Keys.U: return TranslateAlphabetic('u', shift, capsLock);

                case Keys.V: return TranslateAlphabetic('v', shift, capsLock);

                case Keys.W: return TranslateAlphabetic('w', shift, capsLock);

                case Keys.X: return TranslateAlphabetic('x', shift, capsLock);

                case Keys.Y: return TranslateAlphabetic('y', shift, capsLock);

                case Keys.Z: return TranslateAlphabetic('z', shift, capsLock);

                case Keys.D0: return (shift) ? ')' : '0';

                case Keys.D1: return (shift) ? '!' : '1';

                case Keys.D2: return (shift) ? '@' : '2';

                case Keys.D3: return (shift) ? '#' : '3';

                case Keys.D4: return (shift) ? '$' : '4';

                case Keys.D5: return (shift) ? '%' : '5';

                case Keys.D6: return (shift) ? '^' : '6';

                case Keys.D7: return (shift) ? '&' : '7';

                case Keys.D8: return (shift) ? '*' : '8';

                case Keys.D9: return (shift) ? '(' : '9';

                case Keys.Add: return '+';

                case Keys.Divide: return '/';

                case Keys.Multiply: return '*';

                case Keys.Subtract: return '-';

                case Keys.Space: return ' ';

                case Keys.Tab: return '\t';

                case Keys.Decimal: if (numLock && !shift) return '.'; break;

                case Keys.NumPad0: if (numLock && !shift) return '0'; break;

                case Keys.NumPad1: if (numLock && !shift) return '1'; break;

                case Keys.NumPad2: if (numLock && !shift) return '2'; break;

                case Keys.NumPad3: if (numLock && !shift) return '3'; break;

                case Keys.NumPad4: if (numLock && !shift) return '4'; break;

                case Keys.NumPad5: if (numLock && !shift) return '5'; break;

                case Keys.NumPad6: if (numLock && !shift) return '6'; break;

                case Keys.NumPad7: if (numLock && !shift) return '7'; break;

                case Keys.NumPad8: if (numLock && !shift) return '8'; break;

                case Keys.NumPad9: if (numLock && !shift) return '9'; break;

                case Keys.OemBackslash: return shift ? '|' : '\\';

                case Keys.OemCloseBrackets: return shift ? '}' : ']';

                case Keys.OemComma: return shift ? '<' : ',';

                case Keys.OemMinus: return shift ? '_' : '-';

                case Keys.OemOpenBrackets: return shift ? '{' : '[';

                case Keys.OemPeriod: return shift ? '>' : '.';

                case Keys.OemPipe: return shift ? '|' : '\\';

                case Keys.OemPlus: return shift ? '+' : '=';

                case Keys.OemQuestion: return shift ? '?' : '/';

                case Keys.OemQuotes: return shift ? '"' : '\'';

                case Keys.OemSemicolon: return shift ? ':' : ';';

                case Keys.OemTilde: return shift ? '~' : '`';
            }

            return (char)0;
        }

        public static char TranslateAlphabetic(char baseChar, bool shift, bool capsLock)
        {
            return (capsLock ^ shift) ? char.ToUpper(baseChar) : baseChar;
        }

        public void HandleMouseEvents(UpdateState state)
        {
            foreach (var mouse in state.MouseStates) {
                var mouseBtnDown = mouse.MouseState.LeftButton == Microsoft.Xna.Framework.Input.ButtonState.Pressed;
                var mouseDif = mouseBtnDown != mouse.LastMouseDownState;
                if (mouse.NewMultiMouse) mouse.NewMultiMouse = false;
                else
                    mouse.LastMouseDownState = mouseBtnDown;
                state.MouseState = mouse.MouseState; //make sure each event uses the mouse state for this mouse.
                state.CurrentMouseID = mouse.ID;
                //if anyone accesses vanilla mouse state during the update loop, it will be the last mouse that was present.

                var topMost =
                    state.MouseEvents.Where(x => x.Item1 == mouse.ID).OrderByDescending(x => x.Item2.Element.Depth).FirstOrDefault();

                if (topMost != null && !mouse.Dead)
                {

                    /** different element? **/
                    if (mouse.LastMouseOver != topMost.Item2)
                    {

                        if (mouse.LastMouseOver != null)
                        {
                            mouse.LastMouseOver.Callback(UIMouseEventType.MouseOut, state);
                        }

                        topMost.Item2.Callback(UIMouseEventType.MouseOver, state);
                        mouse.LastMouseOver = topMost.Item2;
                    }
                }
                else
                {
                    if (mouse.LastMouseOver != null)
                    {
                        mouse.LastMouseOver.Callback(UIMouseEventType.MouseOut, state);
                        mouse.LastMouseOver = null;
                    }
                }

                if (mouseDif)
                {
                    if (mouseBtnDown)
                    {
                        if (mouse.LastMouseDown != null)
                        {
                            /** We already have mouse down on an object **/
                            return;
                        }
                        if (mouse.LastMouseOver != null)
                        {
                            mouse.LastMouseDown = mouse.LastMouseOver;
                            mouse.LastMouseDown.Callback(UIMouseEventType.MouseDown, state);
                        }
                    }
                    else
                    {
                        if (mouse.LastMouseDown != null)
                        {
                            mouse.LastMouseDown.Callback(UIMouseEventType.MouseUp, state);
                            mouse.LastMouseDown = null;
                        }
                    }
                }
            }

        }

    }

    public class KeyboardInputResult
    {
        public List<Keys> UnhandledKeys = new List<Keys>();
        public bool ContentChanged;
        public bool ShiftDown;
        public bool CapsDown;
        public bool NumLockDown;
        public bool CtrlDown;
        public bool EnterPressed;
        public bool TabPressed;

        public int NumDeletes;
        public int NumInsertions;

        public int SelectionStart;
        public int SelectionEnd;
        public bool SelectionChanged;
    }
}
