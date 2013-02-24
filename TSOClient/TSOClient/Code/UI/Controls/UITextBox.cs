using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using TSOClient.Code.UI.Model;

namespace TSOClient.Code.UI.Controls
{
    public class UITextBox : UIContainer, IFocusableUI
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


        public UITextBox()
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
            m_cursorBlink = true;
            m_cursorBlinkLastTime = GameFacade.LastUpdateState.Time.TotalRealTime.Ticks;
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

                var hasNewInput = state.InputManager.ApplyKeyboardInput(m_SBuilder, state);
                if (hasNewInput)
                {
                    m_cursorBlink = true;
                    m_cursorBlinkLastTime = now;

                    /** We need to recompute the drawing commands **/
                    m_DrawDirty = true;
                }

                /**
                 * Selection?
                 */
                var shiftDown = state.KeyboardState.IsKeyDown(Keys.LeftShift) | 
                                state.KeyboardState.IsKeyDown(Keys.RightShift);

                foreach (var key in state.KeyboardState.GetPressedKeys())
                {
                    switch (key)
                    {
                        case Keys.Left:
                            if (SelectionStart == -1)
                            {
                                SelectionStart = m_SBuilder.Length - 2;
                            }
                            else
                            {
                                SelectionStart--;
                            }
                            if (SelectionStart < 0) { SelectionStart = 0; }
                            m_DrawDirty = true;
                            break;
                    }
                }

            }
        }

        public bool DrawCursor
        {
            get { return m_cursorBlink; }
        }

        private bool m_DrawDirty = false;
        private List<ITextDrawCmd> m_DrawCmds = new List<ITextDrawCmd>();
        private Vector2 m_CursorPosition = Vector2.Zero;

        /// <summary>
        /// When the text / scroll / highlight changes we need to
        /// re-compute how we are going to draw this text field
        /// </summary>
        private void ComputeDrawingCommands()
        {
            m_DrawCmds.Clear();
            m_DrawDirty = false;

            var topLeft = new Vector2(TextMargin.Left, TextMargin.Top);
            var cursorPosition = topLeft;
            var cursorScale = Vector2.One;

            if (SelectionEnd != -1)
            {
            }
            else
            {
                var txt = m_SBuilder.ToString();
                var txtScale = TextStyle.Scale * _Scale;

                m_DrawCmds.Add(new TextDrawCmd_Text
                {
                    Text = txt,
                    Style = TextStyle,
                    Position = LocalPoint(topLeft),
                    Scale = txtScale
                });

                var cursorPrefix = txt;
                if (SelectionStart != -1)
                {
                    cursorPrefix = txt.Substring(0, SelectionStart);
                }

                var stringSize = TextStyle.SpriteFont.MeasureString(cursorPrefix) * TextStyle.Scale;
                cursorPosition = LocalPoint(new Vector2(stringSize.X + topLeft.X, topLeft.Y));
                cursorScale = txtScale;
            }



            m_DrawCmds.Add(new TextDrawCmd_Cursor {
                Text = "|",
                Scale = cursorScale,
                Position = cursorPosition,
                Style = TextStyle
            });
            



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
    }


    public interface ITextDrawCmd
    {
        void Draw(UIElement ui, SpriteBatch batch);
    }

    public class TextDrawCmd_Text : ITextDrawCmd
    {
        public Vector2 Position;
        public string Text;
        public TextStyle Style;
        public Vector2 Scale;

        #region ITextDrawCmd Members
        public virtual void Draw(UIElement ui, SpriteBatch batch)
        {
            batch.DrawString(Style.SpriteFont, Text, Position, Style.Color, 0, Vector2.Zero, Scale, SpriteEffects.None, 0);
        }
        #endregion
    }

    public class TextDrawCmd_Cursor : TextDrawCmd_Text
    {
        public override void Draw(UIElement ui, SpriteBatch batch)
        {
            if (((UITextBox)ui).DrawCursor)
            {
                base.Draw(ui, batch);
            }
        }
    }

}
