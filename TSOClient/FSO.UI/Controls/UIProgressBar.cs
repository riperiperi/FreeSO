using System;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework;
using FSO.Client.UI.Model;
using FSO.Client.GameContent;
using FSO.Common.Utils;
using FSO.Common.Rendering.Framework.Model;
using FSO.Client.UI.Framework.Parser;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// 
    /// </summary>
    public class UIProgressBar : UIElement
    {
        public static ITextureRef StandardBackground;
        public static ITextureRef StandardBar;
        public static TextStyle StandardCaptionStyle;

        public ProgressBarMode Mode = ProgressBarMode.Manual;
        public int DefaultBarWidth = 80;

        public float AnimationPosition = 0;
        public float AnimationDirection = 1;
        public float AnimationDelta = 1;

        static UIProgressBar()
        {
            StandardBackground = new SlicedTextureRef(
                UIElement.GetTexture((ulong)FileIDs.UIFileIDs.dialog_progressbarback),
                new Microsoft.Xna.Framework.Rectangle(13, 13, 13, 13)
            );

            var barTexture = UIElement.GetTexture((ulong)FileIDs.UIFileIDs.dialog_progressbarfront);
            TextureUtils.ManualTextureMask(ref barTexture, new uint[1] { new Color(0x39, 0x51, 0x6B).PackedValue });

            StandardBar = new SlicedTextureRef(barTexture, new Rectangle(18, 7, 18, 7));

            StandardCaptionStyle = TextStyle.DefaultLabel.Clone();
            StandardCaptionStyle.Color = new Color(0, 0, 0);
        }

        private float m_Width;
        private float m_Height;


        public UIProgressBar() : this(StandardBackground, StandardBar)
        {
            BarMargin = new Rectangle(18, 0, 18, 0);
            CaptionStyle = StandardCaptionStyle;
        }

        public UIProgressBar(ITextureRef background, ITextureRef bar)
        {
            this._Background = background;
            this._Bar = bar;
        }

        public string Caption = "{0}%";
        public TextStyle CaptionStyle { get; set; }


        [UIAttribute("size")]
        public override Vector2 Size
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

        private ITextureRef _Background { get; set; }
        private Texture2D _BGTex { get; set; }
        [UIAttribute("backgroundImage")]
        public Texture2D Background
        {
            get
            {
                return _BGTex;
            }
            set
            {
                _BGTex = value;
                _Background = new SlicedTextureRef(value, BarMargin);
                SetSize(value.Width, value.Height);
            }
        }

        private ITextureRef _Bar { get; set; }
        private Texture2D _BarTex { get; set; }
        [UIAttribute("foregroundImage")]
        public Texture2D Bar
        {
            get
            {
                return _BarTex;
            }
            set
            {
                _BarTex = value;
                _Bar = new SlicedTextureRef(value, BarMargin);
            }
        }
        public Rectangle BarMargin = Rectangle.Empty;
        public Rectangle BarOffset = Rectangle.Empty;

        private Rectangle m_Bounds = Rectangle.Empty;

        private float m_MinValue = 0;
        [UIAttribute("minValue")]
        public float MinValue
        {
            get { return m_MinValue; }
            set
            {
                m_MinValue = value;
            }
        }

        private float m_MaxValue = 100;
        [UIAttribute("maxValue")]
        public float MaxValue
        {
            get { return m_MaxValue; }
            set
            {
                m_MaxValue = value;
            }
        }

        private float m_Value = 0;
        public float Value
        {
            get { return m_Value; }
            set
            {
                var newValue = value;
                newValue = Math.Min(newValue, m_MaxValue);
                newValue = Math.Max(newValue, m_MinValue);

                if (m_Value != newValue)
                {
                    m_Value = newValue;
                }
            }
        }

        public float Width
        {
            get { return m_Width; }
        }

        public float Height
        {
            get { return m_Height; }
        }

        public void SetSize(float width, float height)
        {
            m_Width = width;
            m_Height = height;
            m_Bounds = new Rectangle(0, 0, (int)width, (int)height);
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);

            if (Mode == ProgressBarMode.Animated)
            {
                AnimationPosition += (AnimationDelta * AnimationDirection);
                if (AnimationPosition < 0)
                {
                    AnimationPosition = 0;
                    AnimationDirection = 1;
                }
                else if ((AnimationPosition + DefaultBarWidth) >= m_Width)
                {
                    AnimationPosition = m_Width - DefaultBarWidth;
                    AnimationDirection = -1;
                }
            }
        }

        public override void Draw(UISpriteBatch SBatch)
        {
            if (!Visible) return;
            if (_Background != null)
            {
                _Background.Draw(SBatch, this, 0, 0, m_Width, m_Height);
            }


            /** Draw progress bar **/
            var trackSize = m_Width - BarOffset.Right - BarMargin.Right;
            var barHeight = m_Height - BarOffset.Bottom;

            if (Mode == ProgressBarMode.Animated)
            {
                _Bar.Draw(SBatch, this, AnimationPosition, BarOffset.Y, DefaultBarWidth, barHeight);
                return;
            }


            var percent = (m_Value - m_MinValue) / (m_MaxValue - m_MinValue);
            if (m_Value != 0 && _Bar != null)
            {
                var barWidth = BarMargin.Right + (trackSize * percent);
                _Bar.Draw(SBatch, this, BarOffset.Left, BarOffset.Y, barWidth, barHeight);
            }

            /** Draw value label **/
            if (Caption != null && CaptionStyle != null)
            {
                if (!UISpriteBatch.Invalidated)
                {
                    var displayPercent = Math.Round(percent * 100);
                    this.DrawLocalString(SBatch, string.Format(Caption, displayPercent), Vector2.Zero, CaptionStyle, m_Bounds, TextAlignment.Center | TextAlignment.Middle);
                }
            }
        }
    }

    public enum ProgressBarMode
    {
        Manual,

        //No actual progress, just an animated bar
        Animated
    }
}
