using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;
using FSO.Client.Utils;
using FSO.Client.UI.Framework.Parser;
using FSO.Client.UI.Model;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.HIT;
using FSO.Client.GameContent;

namespace FSO.Client.UI.Controls
{
    public delegate void ButtonClickDelegate(UIElement button);

    /// <summary>
    /// A drawable, clickable button that is part of the GUI.
    /// </summary>
    public class UIButton : UIElement
    {
        public static Texture2D StandardButton;

        static UIButton()
        {
            StandardButton = UIElement.GetTexture((ulong)FileIDs.UIFileIDs.buttontiledialog);
        }

        private int m_CurrentFrame;
        private Texture2D m_Texture;

        private TextStyle m_CaptionStyle = TextStyle.DefaultButton;
        private string m_Caption;

        private Rectangle m_Bounds;
        private int m_Width;
        private int m_Height;
        private int m_WidthDiv3;
        private bool m_Disabled;
        private bool m_HighlightNextDraw;
        private float m_ResizeWidth;
        private int m_ImageStates = 4;
        private int m_ButtonFrames = 1;
        private int m_ButtonFrame = 0;
        private int m_AutoMargins = -1;
        private UITooltipHandler m_TooltipHandler;

        private UIElementState m_State = UIElementState.Normal;

        public bool Hovered
        {
            get { return m_isOver; }
        }

        /// <summary>
        /// Sets the margins to be used for automatic button widths. -1 (default) uses the width of the button ends.
        /// </summary>
        public int AutoMargins
        {
            get
            {
                return m_AutoMargins;
            }
            set
            {
                m_AutoMargins = value;
                m_ResizeWidth = 0;
                CalculateAutoSize();
            }
        }

        public UIButton()
            : this(StandardButton)
        {
            UIUtils.GiveTooltip(this);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="Texture"></param>
        public UIButton(Texture2D Texture)
        {
            this.Texture = Texture;

            ClickHandler =
                ListenForMouse(new Rectangle(0, 0, m_Width, m_Height), new UIMouseEvent(OnMouseEvent));

            m_TooltipHandler = UIUtils.GiveTooltip(this); //buttons can have tooltips
        }

        [UIAttribute("size")]
        public override Vector2 Size
        {
            get
            {
                return new Vector2(Width == 0 ? m_Width : Width, m_Height);
            }
            set
            {
                Width = value.X;
            }
        }

        [UIAttribute("imageStates")]
        public int ImageStates
        {
            get
            {
                return m_ImageStates;
            }
            set
            {
                m_ImageStates = value; //recalculate button offsets
                m_Width = m_Texture.Width/m_ImageStates;
                m_Height = m_Texture.Height / m_ButtonFrames;
                m_WidthDiv3 = m_Width / 3;

                if (ClickHandler != null)
                {
                    ClickHandler.Region.Width = (m_ResizeWidth == 0) ? m_Width : (int)m_ResizeWidth;
                    ClickHandler.Region.Height = m_Height;
                }
            }
        }

        public int ButtonFrame
        {
            get { return m_ButtonFrame; }
            set { m_ButtonFrame = value; }
        }
        
        public int ButtonFrames
        {
            get
            {
                return m_ButtonFrames;
            }
            set
            {
                m_ButtonFrames = value; //recalculate button offsets
                m_Width = m_Texture.Width / m_ImageStates;
                m_Height = m_Texture.Height / m_ButtonFrames;
                m_WidthDiv3 = m_Width / 3;

                if (ClickHandler != null)
                {
                    ClickHandler.Region.Width = (m_ResizeWidth == 0) ? m_Width : (int)m_ResizeWidth;
                    ClickHandler.Region.Height = m_Height;
                }
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
                CalculateAutoSize();
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

        protected UIMouseEventRef ClickHandler;

        public void InflateHitbox(int x, int y)
        {
            ClickHandler.Region.Inflate(x, y);
        }

        public void DeregisterHandler()
        {
            RemoveMouseListener(ClickHandler);
        }

        [UIAttribute("image")]
        public Texture2D Texture 
        {
            get { return m_Texture; }
            set {
                m_Texture = value;
                m_Bounds = Rectangle.Empty;

                m_Width = m_Texture.Width / m_ImageStates;
                m_WidthDiv3 = m_Width / 3;
                m_Height = m_Texture.Height / m_ButtonFrames;
                m_CurrentFrame = 0;

                if (ClickHandler != null)
                {
                    ClickHandler.Region.Width = (m_ResizeWidth == 0) ? m_Width : (int)m_ResizeWidth;
                    ClickHandler.Region.Height = m_Height;
                }
            } 
        }
        public void ActivateTooltip()
        {
            m_TooltipHandler = UIUtils.GiveTooltip(this); //buttons can have tooltips
        }
        private bool m_CalcAutoSize;
        private void CalculateAutoSize()
        {
            m_CalcAutoSize = false;
            if (m_ResizeWidth == 0)
            {
                /** Measure the text **/
                var size = m_CaptionStyle.MeasureString(m_Caption);

                if (m_AutoMargins == -1) Width = (m_WidthDiv3 * 2) + (int)size.X;
                else Width = m_AutoMargins*2 + (int)size.X;
            }
        }

        private void CalculateState() {
            if (m_Disabled) { 
                m_State = UIElementState.Disabled;
                return;
            }

            m_State = UIElementState.Normal;
            switch(m_CurrentFrame){
                case 1:
                    m_State = UIElementState.Selected;
                    break;

                case 2:
                    m_State = UIElementState.Highlighted;
                    break;
            }
            
        }

        public event ButtonClickDelegate OnButtonClick;
        public event ButtonClickDelegate OnButtonHover;
        public event ButtonClickDelegate OnButtonDown;
        public event ButtonClickDelegate OnButtonExit;

        public bool Highlight
        {
            set { m_HighlightNextDraw = value; }
        }

        public bool Disabled
        {
            get { return m_Disabled; }
            set { if (m_Disabled == value) { return; } m_Disabled = value; Invalidate(); CalculateState(); }
        }

        private bool _Selected;
        public bool Selected
        {
            get { return _Selected; }
            set { if (_Selected == value) { return; } _Selected = value; Invalidate(); }
        }

        public int ForceState = -1;

        /// <summary>
        /// Gets or sets the current frame for this button.
        /// </summary>
        public int CurrentFrame
        {
            get
            {
                return m_CurrentFrame;
            }

            set
            {
                //Frames go from 0 to 3.
                if(value < 4 && m_CurrentFrame != value)
                {
                    Invalidate();
                    m_CurrentFrame = value;
                    CalculateState();
                }
            }
        }

        public int CurrentFrameIndex
        {
            get { return m_CurrentFrame; }
        }

        private bool m_isOver;
        private bool m_isDown;

        public bool IsDown
        {
            get { return m_isDown; }
        }

        protected void OnMouseEvent(UIMouseEventType type, UpdateState state)
        {
            if ((m_Disabled || Opacity < 1f) && type != UIMouseEventType.MouseOut) { return; }
            Invalidate();
            switch (type)
            {
                case UIMouseEventType.MouseOver:
                    m_isOver = true;
                    if (!m_isDown)
                    {
                        CurrentFrame = 2;
                        if (OnButtonHover != null)
                        {
                            OnButtonHover(this);
                        }
                    }
                    break;

                case UIMouseEventType.MouseOut:
                    m_isOver = false;
                    if (!m_isDown)
                    {
                        CurrentFrame = 0;
                        if (OnButtonExit != null)
                        {
                            OnButtonExit(this);
                        }
                    }
                    break;

                case UIMouseEventType.MouseDown:
                    m_isDown = true;
                    CurrentFrame = 1;
                    if (OnButtonDown != null)
                        OnButtonDown(this);
                    break;

                case UIMouseEventType.MouseUp:
                    if (m_isDown)
                    {
                        if (OnButtonClick != null)
                        {
                            OnButtonClick(this);
                            HITVM.Get().PlaySoundEvent(UISounds.Click);
                        }
                    }
                    m_isDown = false;
                    CurrentFrame = m_isOver ? 2 : 0;
                    break;
            }
        }

        public override void Draw(UISpriteBatch SBatch)
        {
            if (!Visible) { return; }

            if (m_CalcAutoSize)
            {
                CalculateAutoSize();
            }

            /** Draw the button as a 3 slice **/
            var frame = m_CurrentFrame;
            if (m_Disabled)
            {
                frame = (m_ImageStates < 4)?0:3;
            }
            if (Selected)
            {
                frame = 1;
            }
            if (ForceState > -1) frame = ForceState;
            frame = Math.Min(m_ImageStates - 1, frame);
            int offset = frame * m_Width;
            int vOffset = m_ButtonFrame * m_Height;

            if (Width != 0)
            {
                //TODO: Work out these numbers once & cache them. Invalidate when texture or width changes

                /** left **/
                base.DrawLocalTexture(SBatch, m_Texture, new Rectangle(offset, vOffset, m_WidthDiv3, m_Height), Vector2.Zero);

                /** center **/
                base.DrawLocalTexture(SBatch, m_Texture, new Rectangle(offset + m_WidthDiv3, vOffset, m_WidthDiv3, m_Height), new Vector2(m_WidthDiv3, 0), new Vector2( (Width - (m_WidthDiv3 * 2)) / m_WidthDiv3, 1.0f));

                /** right **/
                base.DrawLocalTexture(SBatch, m_Texture, new Rectangle(offset + (m_Width - m_WidthDiv3), vOffset, m_WidthDiv3, m_Height), new Vector2(Width - m_WidthDiv3, 0));
            }
            else
            {
                base.DrawLocalTexture(SBatch, m_Texture, new Rectangle(offset, vOffset, m_Width, m_Height), Vector2.Zero);
            }

            /**
             * Label
             */
            if (m_Caption != null && m_CaptionStyle != null)
            {
                var box =GetBounds();
                //Little hack to get slightly better centering on text on buttons
                box.Height -= 2;
                this.DrawLocalString(SBatch, m_Caption, Vector2.Zero, m_CaptionStyle, box, TextAlignment.Center | TextAlignment.Middle, Rectangle.Empty, m_State);
            }
        }

        public override Rectangle GetBounds()
        {
            /*
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
            */
            return new Rectangle(0, 0, ClickHandler.Region.Width, ClickHandler.Region.Height); //ClickHandler.Region seems to be infinitely more trustworthy for this.
        }
    }
}
