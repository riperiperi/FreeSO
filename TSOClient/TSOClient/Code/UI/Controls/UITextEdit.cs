using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TSOClient.Code.UI.Model;
using TSOClient.Code.Utils;


namespace TSOClient.Code.UI.Controls
{
    /// <summary>
    /// Big complex text edit control, used in many places
    /// </summary>
    public class UITextEdit : UIElement, IFocusableUI, ITextControl
    {
        /**
         * Background texture & resize info
         */
        private Texture2D m_BackgroundTex;
        private NineSliceMargins NineSliceMargins;
        private float m_Width;
        private float m_Height;

        /**
         * Text box vars
         */
        private StringBuilder m_SBuilder = new StringBuilder();


        /**
         * Interaction
         */
        private UIMouseEventRef m_MouseEvent;

        private int SelectionStart = -1;
        private int SelectionEnd = -1;


        public UITextEdit()
        {
            this.SetBackgroundTexture(
                GetTexture((ulong)TSOClient.FileIDs.UIFileIDs.dialog_textboxbackground),
                13, 13, 13, 13);

            TextMargin = new Rectangle(8, 3, 8, 5);

            m_MouseEvent = ListenForMouse(new Rectangle(0, 0, 10, 10), new UIMouseEvent(OnMouseEvent));
        }


        


        /**
         * Functionality
         */

        /// <summary>
        /// Returns the current text (input) in this textbox.
        /// </summary>
        public string CurrentText
        {
            get { return m_SBuilder.ToString(); }
            set
            {
                m_SBuilder = new StringBuilder(value);
                m_DrawDirty = true;
            }
        }


        public TextStyle TextStyle = TextStyle.DefaultLabel;
        public Rectangle TextMargin = Rectangle.Empty;

        /**
         * Properties
         */
        
        /// <summary>
        /// Background texture
        /// </summary>
        public Texture2D BackgroundTexture
        {
            get { return m_BackgroundTex; }
        }


        public void SetBackgroundTexture(Texture2D texture, int marginLeft, int marginRight, int marginTop, int marginBottom)
        {
            m_BackgroundTex = texture;
            if (texture != null)
            {
                NineSliceMargins = new NineSliceMargins
                {
                    Left = marginLeft,
                    Right = marginRight,
                    Top = marginTop,
                    Bottom = marginBottom
                };
                NineSliceMargins.CalculateOrigins(texture);
            }
            else
            {
                NineSliceMargins = null;
            }
        }

        /// <summary>
        /// Component width
        /// </summary>
        public float Width
        {
            get { return m_Height; }
        }

        /// <summary>
        /// Component height
        /// </summary>
        public float Height
        {
            get { return m_Height; }
        }


        public void SetSize(float width, float height)
        {
            m_Width = width;
            m_Height = height;
            
            NineSliceMargins.CalculateScales(m_Width, m_Height);
            m_Bounds = new Rectangle(0, 0, (int)m_Width, (int)m_Height);
            
            if (m_MouseEvent != null)
            {
                m_MouseEvent.Region = new Rectangle(0, 0, (int)m_Width, (int)m_Height);
            }
        }

        private Rectangle m_Bounds;
        public override Rectangle GetBounds()
        {
            return m_Bounds;
        }


        /**
         * Interaction Functionality
         */
        public void OnMouseEvent(UIMouseEventType evt, UpdateState state)
        {
            switch (evt)
            {
                case UIMouseEventType.MouseUp:
                    state.InputManager.SetFocus(this);
                    break;

                case UIMouseEventType.MouseOver:
                    break;

                case UIMouseEventType.MouseOut:
                    break;
            }
        }



        #region IFocusableUI Members

        private bool IsFocused;
        public void OnFocusChanged(FocusEvent newFocus)
        {
            IsFocused = newFocus == FocusEvent.FocusIn;
            if (IsFocused)
            {
                m_cursorBlink = true;
                m_cursorBlinkLastTime = GameFacade.LastUpdateState.Time.TotalRealTime.Ticks;
            }
            else
            {
                m_cursorBlink = false;
            }
        }

        #endregion

        private bool m_cursorBlink = false;
        private long m_cursorBlinkLastTime;

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (IsFocused)
            {
                /**
                 * TODO: Selection management
                 */
                var now = state.Time.TotalRealTime.Ticks;
                if (now - m_cursorBlinkLastTime > 5000000)
                {
                    m_cursorBlinkLastTime = now;
                    m_cursorBlink = !m_cursorBlink;
                }

                var inputResult = state.InputManager.ApplyKeyboardInput(m_SBuilder, state, SelectionStart, SelectionEnd);
                if (inputResult != null)
                {
                    if (inputResult.ContentChanged)
                    {
                        m_cursorBlink = true;
                        m_cursorBlinkLastTime = now;

                        /** We need to recompute the drawing commands **/
                        m_DrawDirty = true;

                        if (SelectionStart != -1)
                        {
                            SelectionStart += inputResult.NumInsertions;
                            SelectionStart -= inputResult.NumDeletes;
                        }
                    }

                    /**
                     * Selection?
                     */
                    foreach (var key in inputResult.UnhandledKeys)
                    {
                        switch (key)
                        {
                            case Keys.Left:
                                if (inputResult.ShiftDown)
                                {
                                    /** SelectionEnd**/
                                    if (SelectionEnd == -1)
                                    {
                                        if (SelectionStart != -1)
                                        {
                                            SelectionEnd = SelectionStart;
                                        }
                                        else
                                        {
                                            SelectionEnd = m_SBuilder.Length;
                                        }
                                    }
                                    SelectionEnd--;
                                    SelectionEnd = Math.Max(SelectionEnd, 0);
                                    if (SelectionEnd >= m_SBuilder.Length) { SelectionEnd = -1; }

                                    /** Selection size = 0, act as if there is no selection **/
                                    if (SelectionEnd == SelectionStart)
                                    {
                                        SelectionEnd = -1;
                                    }
                                }
                                else
                                {
                                    if (SelectionEnd != -1)
                                    {
                                        SelectionStart = SelectionEnd;
                                        SelectionEnd = -1;
                                    }
                                    if (SelectionStart == -1)
                                    {
                                        SelectionStart = m_SBuilder.Length - 1;
                                    }
                                    else
                                    {
                                        SelectionStart--;
                                    }
                                    if (SelectionStart < 0) { SelectionStart = 0; }
                                }
                                m_DrawDirty = true;
                                break;

                            case Keys.Right:
                                if (inputResult.ShiftDown)
                                {
                                    /** SelectionEnd**/
                                    if (SelectionEnd == -1)
                                    {
                                        if (SelectionStart != -1)
                                        {
                                            SelectionEnd = SelectionStart;
                                        }
                                        else
                                        {
                                            SelectionEnd = m_SBuilder.Length;
                                        }
                                    }
                                    SelectionEnd++;
                                    if (SelectionEnd >= m_SBuilder.Length) { SelectionEnd = -1; }

                                    /** Selection size = 0, act as if there is no selection **/
                                    if (SelectionEnd == SelectionStart)
                                    {
                                        SelectionEnd = -1;
                                    }
                                }
                                else
                                {
                                    if (SelectionEnd != -1)
                                    {
                                        SelectionStart = SelectionEnd;
                                        SelectionEnd = -1;
                                    }

                                    if (SelectionStart != -1)
                                    {
                                        SelectionStart++;
                                        if (SelectionStart >= m_SBuilder.Length)
                                        {
                                            SelectionStart = -1;
                                        }
                                    }
                                }
                                m_DrawDirty = true;
                                break;
                        }
                    }

                }
            }
        }

        private bool m_DrawDirty = false;
        private List<ITextDrawCmd> m_DrawCmds = new List<ITextDrawCmd>();
        private List<UITextEditLine> m_Lines = new List<UITextEditLine>();
        private Vector2 m_CursorPosition = Vector2.Zero;

        public UITextEditLine GetLineForIndex(int index)
        {
            foreach (var line in m_Lines)
            {
                if (index >= line.StartIndex && index <= line.StartIndex + line.Text.Length)
                {
                    return line;
                }
            }
            return null;
        }


        /// <summary>
        /// When the text / scroll / highlight changes we need to
        /// re-compute how we are going to draw this text field
        /// </summary>
        private void ComputeDrawingCommands()
        {
            m_DrawCmds.Clear();
            m_DrawDirty = false;

            /**
             * Split the text into lines using manual lines
             * breaks and word wrap
             */
            var txt = m_SBuilder.ToString();
            var lineWidth = m_Width - (TextMargin.Left + TextMargin.Height);
            var lineHeight = TextStyle.MeasureString("W").Y;

            m_Lines.Clear();

            var words = txt.Split(' ');
	        var spaceWidth = TextStyle.MeasureString(" ").X;



            var currentLine = new StringBuilder();
            var currentLineWidth = 0.0f;
            var currentLineNum = 0;

            for (var i = 0; i < words.Length; i++)
            {
                var word = words[i];

                if (word == "\r\n")
                {
                    /** Line break **/
                    /*lines.Add(new UITextEditLine
                    {
                        Text = currentLine.ToString(),
                        LineWidth = currentLineWidth
                    });
                    currentLine = new StringBuilder();
                    currentLineWidth = 0.0f;*/
                }
                else
                {
                    var wordSize = TextStyle.MeasureString(word);

                    if (currentLineWidth + wordSize.X < lineWidth)
                    {
                        currentLine.Append(word);
                        currentLine.Append(' ');
                        currentLineWidth += wordSize.X;
                        currentLineWidth += spaceWidth;
                    }
                    else
                    {
                        /** New line **/
                        m_Lines.Add(new UITextEditLine
                        {
                            Text = currentLine.ToString(),
                            LineWidth = currentLineWidth,
                            LineNumber = currentLineNum
                        });
                        currentLineNum++;
                        currentLine = new StringBuilder();
                        currentLine.Append(word);
                        currentLine.Append(' ');

                        currentLineWidth = wordSize.X + spaceWidth;
                    }
                }
            }

            if(currentLine.Length > 0){
                m_Lines.Add(new UITextEditLine
                {
                    Text = currentLine.ToString(),
                    LineWidth = currentLineWidth,
                    LineNumber = currentLineNum
                });
            }


            var topLeft = new Vector2(TextMargin.Left, TextMargin.Top);
            var position = topLeft;
            var txtScale = TextStyle.Scale * _Scale;
            var currentIndex = 0;
            foreach (var line in m_Lines)
            {
                line.StartIndex = currentIndex;
                currentIndex += line.Text.Length;
            }


            /** selection **/
            var start = SelectionStart == -1 ? m_SBuilder.Length : SelectionStart;
            var end = SelectionEnd == -1 ? m_SBuilder.Length : SelectionEnd;


            foreach (var line in m_Lines)
            {
                m_DrawCmds.Add(new TextDrawCmd_Text
                {
                    Text = line.Text,
                    Style = TextStyle,
                    Position = LocalPoint(position),
                    Scale = txtScale
                });

                position.Y += lineHeight;
            }

            var cursorLine = GetLineForIndex(start);
            if (cursorLine != null)
            {
                var prefix = start - cursorLine.StartIndex;
                var cursorPosition = new Vector2(topLeft.X, topLeft.Y + (cursorLine.LineNumber * lineHeight));

                if (prefix > 0)
                {
                    cursorPosition.X += TextStyle.MeasureString(cursorLine.Text.Substring(0, prefix)).X;
                }


                m_DrawCmds.Add(new TextDrawCmd_Cursor
                {
                    Scale = new Vector2(_Scale.X, lineHeight * _Scale.Y),
                    Position = LocalPoint(cursorPosition),
                    Texture = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, TextStyle.Color)
                });
            }


            //if (true) { return; }

            //var cursorPosition = topLeft;


            //if (SelectionEnd != -1)
            //{
            //    var start = SelectionStart == -1 ? m_SBuilder.Length : SelectionStart;
            //    var end = SelectionEnd;
            //    if (end < start) {
            //        var temp = start;
            //        start = end;
            //        end = temp;
            //    }

            //    var prefixSize = Vector2.Zero;
            //    if (start > 0)
            //    {
            //        /** Prefix **/
            //        var prefix = txt.Substring(0, start);
            //        prefixSize = TextStyle.SpriteFont.MeasureString(prefix) * TextStyle.Scale;

            //        m_DrawCmds.Add(new TextDrawCmd_Text
            //        {
            //            Text = prefix,
            //            Style = TextStyle,
            //            Position = LocalPoint(topLeft),
            //            Scale = txtScale
            //        });
            //    }


            //    /** Selection text **/
            //    var selectionTxt = txt.Substring(start, end - start);
            //    var selectionPosition = LocalPoint(new Vector2(prefixSize.X + topLeft.X, topLeft.Y));
            //    var selectionTxtSize = TextStyle.SpriteFont.MeasureString(selectionTxt) * TextStyle.Scale;

            //    /** Selection box **/
            //    m_DrawCmds.Add(new TextDrawCmd_SelectionBox {
            //        BlendColor = new Color(0xFF, 0xFF, 0xFF, 200),
            //        Texture = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, TextStyle.SelectionBoxColor),
            //        Position = selectionPosition,
            //        Scale = new Vector2(selectionTxtSize.X, selectionTxtSize.Y) * _Scale
            //    });
                
            //    m_DrawCmds.Add(new TextDrawCmd_Text
            //    {
            //        Selected = true,
            //        Text = selectionTxt,
            //        Style = TextStyle,
            //        Position = selectionPosition,
            //        Scale = txtScale
            //    });


            //    if (end < txt.Length)
            //    {
            //        /** Suffix **/
            //        m_DrawCmds.Add(new TextDrawCmd_Text
            //        {
            //            Text = txt.Substring(end),
            //            Style = TextStyle,
            //            Position = LocalPoint(new Vector2(prefixSize.X + selectionTxtSize.X + topLeft.X, topLeft.Y)),
            //            Scale = txtScale
            //        });
            //    }
            //}
            //else
            //{
            //    m_DrawCmds.Add(new TextDrawCmd_Text
            //    {
            //        Text = txt,
            //        Style = TextStyle,
            //        Position = LocalPoint(topLeft),
            //        Scale = txtScale
            //    });

            //    var cursorPrefix = txt;
            //    if (SelectionStart != -1)
            //    {
            //        cursorPrefix = txt.Substring(0, SelectionStart);
            //    }

            //    var stringSize = TextStyle.SpriteFont.MeasureString(cursorPrefix) * TextStyle.Scale;
            //    cursorPosition = LocalPoint(new Vector2(stringSize.X + topLeft.X, topLeft.Y));
            //}



            //m_DrawCmds.Add(new TextDrawCmd_Cursor
            //{
            //    Scale = new Vector2(_Scale.X, (m_Height-(TextMargin.Top + TextMargin.Height)) * _Scale.Y),
            //    Position = cursorPosition,
            //    Texture = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, TextStyle.Color)
            //});




            //var str = m_SBuilder.ToString();
            //if (IsFocused)
            //{
                //if (SelectionStart != -1)
                //{
                    /** We need to draw selection! **/
                //}

                //if (m_cursorBlink)
                //{
                //    str += "|";
                //}
            //}
        }



        /// <summary>
        /// Render
        /// </summary>
        /// <param name="batch"></param>
        public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch batch)
        {
            if (m_DrawDirty)
            {
                ComputeDrawingCommands();
            }

            /** Can have a text box without a background **/
            if (m_BackgroundTex != null && NineSliceMargins != null)
            {
                NineSliceMargins.DrawOnto(batch, this, m_BackgroundTex, m_Width, m_Height);
            }
            
            /**
             * Draw text
             */
            foreach (var cmd in m_DrawCmds)
            {
                cmd.Draw(this, batch);
            }
        }



        protected override void CalculateMatrix()
        {
            base.CalculateMatrix();
            m_DrawDirty = true;
        }



        #region ITextControl Members

        bool ITextControl.DrawCursor
        {
            get
            {
                return IsFocused && m_cursorBlink;
            }
        }

        #endregion
    }



    public class UITextEditLine
    {
        public int StartIndex;
        public int LineNumber;
        public string Text;
        public float LineWidth;
        public float LineHeight;
    }

}
