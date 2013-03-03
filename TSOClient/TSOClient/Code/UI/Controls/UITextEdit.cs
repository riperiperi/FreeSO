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
using TSOClient.Code.UI.Framework.Parser;


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
            TextStyle = TextStyle.DefaultLabel;

            //this.SetBackgroundTexture(
            //    GetTexture((ulong)TSOClient.FileIDs.UIFileIDs.dialog_textboxbackground),
            //    13, 13, 13, 13);

            //TextMargin = new Rectangle(8, 3, 8, 5);

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


        [UIAttribute("font", typeof(TextStyle))]
        public TextStyle TextStyle { get; set; }
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

        [UIAttribute("size")]
        public Vector2 Size
        {
            get
            {
                return new Vector2(m_Width, m_Height);
            }
            set
            {
                SetSize(value.X, value.Y);
            }
        }

        public void SetSize(float width, float height)
        {
            m_Width = width;
            m_Height = height;

            if (NineSliceMargins != null)
            {
                NineSliceMargins.CalculateScales(m_Width, m_Height);
            }
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


        private bool m_IsDraggingSelection = false;

        /**
         * Interaction Functionality
         */
        public void OnMouseEvent(UIMouseEventType evt, UpdateState state)
        {
            switch (evt)
            {
                case UIMouseEventType.MouseDown:
                    /**
                     * Hit test, work out where selection should begin
                     */
                    var position = this.GetMousePosition(state.MouseState);
                    var index = this.HitTestText(position);
                    SelectionStart = index;
                    SelectionEnd = -1;
                    m_DrawDirty = true;
                    m_IsDraggingSelection = true;

                    state.InputManager.SetFocus(this);
                    break;

                case UIMouseEventType.MouseOver:
                    break;

                case UIMouseEventType.MouseOut:
                    break;

                case UIMouseEventType.MouseUp:
                    m_IsDraggingSelection = false;
                    break;
            }
        }

        /// <summary>
        /// Returns which character index would be hit
        /// by the given mouse coordinates. The coords should be
        /// in local coords.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public int HitTestText(Vector2 point)
        {
            var yPosition = (float)TextMargin.Y;

            for (var i = 0; i < m_Lines.Count; i++)
            {
                var line = m_Lines[i];

                if (point.Y >= yPosition && point.Y <= yPosition + m_LineHeight)
                {
                    /** Its this line! **/
                    /** Now we need to work out what the X coordinate relates to **/
                    var roughEst =
                        Math.Round(
                            ((point.X - TextMargin.X) / line.LineWidth) * line.Text.Length
                        );
                    var index = Math.Max(0, roughEst);
                    index = Math.Min(index, line.Text.Length);

                    return (int)line.StartIndex + (int)index;
                }
                yPosition += m_LineHeight;
            }
            return -1;
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
                SelectionEnd = -1;
                SelectionStart = -1;
                m_DrawDirty = true;
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
                    SelectionStart = inputResult.SelectionStart;
                    SelectionEnd = inputResult.SelectionEnd;

                    if (inputResult.ContentChanged || inputResult.SelectionChanged)
                    {
                        m_cursorBlink = true;
                        m_cursorBlinkLastTime = now;

                        /** We need to recompute the drawing commands **/
                        m_DrawDirty = true;
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



                if (m_IsDraggingSelection)
                {
                    /** Dragging **/
                    var position = this.GetMousePosition(state.MouseState);
                    var index = this.HitTestText(position);
                    SelectionEnd = index;
                    if (SelectionEnd == SelectionStart)
                    {
                        SelectionEnd = -1;
                    }

                    m_DrawDirty = true;
                }
            }
        }

        private bool m_DrawDirty = false;
        private List<ITextDrawCmd> m_DrawCmds = new List<ITextDrawCmd>();
        private List<UITextEditLine> m_Lines = new List<UITextEditLine>();
        private Vector2 m_CursorPosition = Vector2.Zero;
        private float m_LineHeight;

        public UITextEditLine GetLineForIndex(int index)
        {
            foreach (var line in m_Lines)
            {
                if (index >= line.StartIndex && index < line.StartIndex + line.Text.Length)
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
            m_LineHeight = TextStyle.MeasureString("W").Y;

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

            var yPosition = topLeft.Y;
            foreach (var line in m_Lines)
            {
                var segments = CalculateSegments(line);
                var xPosition = topLeft.X;

                foreach (var segment in segments)
                {
                    var segmentSize = TextStyle.MeasureString(segment.Text);
                    var segmentPosition = LocalPoint(new Vector2(xPosition, yPosition));

                    if (segment.Selected)
                    {
                        m_DrawCmds.Add(new TextDrawCmd_SelectionBox
                        {
                            BlendColor = new Color(0xFF, 0xFF, 0xFF, 200),
                            Texture = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, TextStyle.SelectionBoxColor),
                            Position = segmentPosition,
                            Scale = new Vector2(segmentSize.X, m_LineHeight) * _Scale
                        });
                    }

                    m_DrawCmds.Add(new TextDrawCmd_Text
                    {
                        Selected = segment.Selected,
                        Text = segment.Text,
                        Style = TextStyle,
                        Position = segmentPosition,
                        Scale = txtScale
                    });

                    xPosition += segmentSize.X;
                }

                yPosition += m_LineHeight;

                position.Y += m_LineHeight;
            }

            var start = SelectionStart == -1 ? m_SBuilder.Length : SelectionStart;
            var cursorLine = GetLineForIndex(start);
            if (cursorLine != null)
            {
                var prefix = start - cursorLine.StartIndex;
                var cursorPosition = new Vector2(topLeft.X, topLeft.Y + (cursorLine.LineNumber * m_LineHeight));

                if (prefix > 0)
                {
                    cursorPosition.X += TextStyle.MeasureString(cursorLine.Text.Substring(0, prefix)).X;
                }

                m_DrawCmds.Add(new TextDrawCmd_Cursor
                {
                    Scale = new Vector2(_Scale.X, m_LineHeight * _Scale.Y),
                    Position = LocalPoint(cursorPosition),
                    Texture = TextureUtils.TextureFromColor(GameFacade.GraphicsDevice, TextStyle.CursorColor)
                });
            }
        }

        /// <summary>
        /// Creates a list of segments to split the line into
        /// in order to draw selection boxes
        /// </summary>
        /// <returns></returns>
        protected List<UITextEditLineSegment> CalculateSegments(UITextEditLine line)
        {
            var result = new List<UITextEditLineSegment>();

            if (SelectionEnd != -1)
            {
                /** There is a selection **/
                var start = SelectionStart == -1 ? m_SBuilder.Length : SelectionStart;
                var end = SelectionEnd == -1 ? m_SBuilder.Length : SelectionEnd;
                if (end < start)
                {
                    var temp = start;
                    start = end;
                    end = temp;
                }

                var lineStart = line.StartIndex;
                var lineEnd = lineStart + line.Text.Length;

                /**
                 * Options:
                 *  This line has no selection,
                 *  Selection starts on this line
                 *  Selection ends on this line
                 *  The whole line is selected
                 */

                if (start >= lineStart && start < lineEnd)
                {
                    /** Selection starts on this line, we need a prefix **/
                    var prefixEnd = start - lineStart;
                    if (prefixEnd != 0)
                    {
                        result.Add(new UITextEditLineSegment
                        {
                            Selected = false,
                            Text = line.Text.Substring(0, prefixEnd)
                        });
                    }

                    /** Up until the end **/
                    var selectionEnd = line.Text.Length;
                    if (end + 1 < lineEnd)
                    {
                        selectionEnd -= (lineEnd - end);
                    }
                    
                    result.Add(new UITextEditLineSegment
                    {
                        Selected = true,
                        Text = line.Text.Substring(prefixEnd, selectionEnd - prefixEnd)
                    });

                    /** Suffix? **/
                    if (end + 1 < lineEnd)
                    {
                        result.Add(new UITextEditLineSegment
                        {
                            Selected = false,
                            Text = line.Text.Substring(selectionEnd)
                        });
                    }
                }
                else if (start < lineStart && end >= lineStart && end <= lineEnd)
                {
                    /** Selection ends on this line **/
                    /** Up until the end **/
                    var selectionEnd = line.Text.Length;
                    if (end + 1 < lineEnd)
                    {
                        selectionEnd -= (lineEnd - end);
                    }

                    result.Add(new UITextEditLineSegment
                    {
                        Selected = true,
                        Text = line.Text.Substring(0, selectionEnd)
                    });

                    /** Suffix? **/
                    if (end + 1 < lineEnd)
                    {
                        result.Add(new UITextEditLineSegment
                        {
                            Selected = false,
                            Text = line.Text.Substring(selectionEnd)
                        });
                    }
                }
                else if (start < lineStart && end > lineEnd)
                {
                    /** The whole line is selected **/
                    result.Add(new UITextEditLineSegment
                    {
                        Text = line.Text,
                        Selected = true
                    });
                }
                else
                {
                    result.Add(new UITextEditLineSegment
                    {
                        Text = line.Text,
                        Selected = false
                    });
                }

                //if (lineStart >= start && lineEnd <= end)
                //{
                //    /** Part of this line is selected **/
                //    result.Add(new UITextEditLineSegment
                //    {
                //        Selected = true,
                //        Text = line.Text
                //    });
                //}


            }
            else
            {
                result.Add(new UITextEditLineSegment
                {
                    Text = line.Text,
                    Selected = false
                });
            }
            return result;
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

    public class UITextEditLineSegment
    {
        public string Text;
        public bool Selected;
    }

}
