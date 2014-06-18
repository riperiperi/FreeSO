/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

The Original Code is the TSOClient.

The Initial Developer of the Original Code is
ddfczm. All Rights Reserved.

Contributor(s): ______________________________________.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Framework.Parser;
using Microsoft.Xna.Framework;
using TSOClient.Code.Utils;
using TSOClient.Code.UI.Model;
using TSOClient.LUI;
using TSO.Common.rendering.framework.model;
using TSO.Common.rendering.framework.io;

namespace TSOClient.Code.UI.Controls
{
    public delegate void ChangeDelegate(UIElement element);

    public class UISlider : UIElement
    {
        private MathCache m_LayoutCache = new MathCache();
        private Texture2D m_Texture;

        /** Mouse handler for the thumb button **/
        private UIMouseEventRef m_ThumbEvent;

        public event ChangeDelegate OnChange;
        public event ChangeDelegate OnRangeChange;


        [UIAttribute("image")]
        public Texture2D Texture
        {
            get
            {
                return m_Texture;
            }
            set
            {
                m_Texture = value;
                m_LayoutCache.Invalidate();
            }
        }

        [UIAttribute("orientation")]
        public int Orientation
        {
            get
            {
                return _Orientation;
            }
            set
            {
                _Orientation = value;
            }
        }

        private int _Orientation = 1; //0 is horizontal, 1 is vertical

        private float m_Width;
        private float m_Height;

        [UIAttribute("size")]
        public override Vector2 Size
        {
            set
            {
                SetSize(value.X, value.Y);
            }
        }


        protected override void CalculateMatrix()
        {
            base.CalculateMatrix();
            m_LayoutCache.Invalidate();
        }



        /**
         * Scroll values
         */
        public bool AllowDecimals = false;

        private float m_MinValue = 0;
        public float MinValue
        {
            get { return m_MinValue; }
            set
            {
                m_MinValue = value;
                if (OnRangeChange != null)
                {
                    OnRangeChange(this);
                }
            }
        }

        private float m_Value = 10;
        public float Value
        {
            get { return m_Value; }
            set
            {
                var newValue = value;
                if (AllowDecimals == false)
                {
                    newValue = (float)Math.Round(newValue);
                }
                newValue = Math.Min(newValue, m_MaxValue);
                newValue = Math.Max(newValue, m_MinValue);

                if (m_Value != newValue)
                {
                    m_Value = newValue;
                    System.Diagnostics.Debug.WriteLine(newValue);
                    m_LayoutCache.Invalidate("btn");

                    if (OnChange != null)
                    {
                        OnChange(this);
                    }
                }

            }
        }

        private float m_MaxValue = 10;
        public float MaxValue
        {
            get { return m_MaxValue; }
            set
            {
                m_MaxValue = value;
                if (OnRangeChange != null)
                {
                    OnRangeChange(this);
                }
            }
        }




        public UISlider()
        {
            m_ThumbEvent = this.ListenForMouse(new Rectangle(0, 0, 0, 0), new UIMouseEvent(OnThumbClick));
        }

        private bool m_ThumbDown;
        private Vector2 m_ThumbMouseOffset;

        private void OnThumbClick(UIMouseEventType type, UpdateState state)
        {
            switch (type)
            {
                case UIMouseEventType.MouseDown:
                    m_ThumbDown = true;
                    m_ThumbMouseOffset = this.GetMousePosition(state.MouseState);

                    var layout = m_LayoutCache.Calculate("layout", x => CalculateLayout());
                    var buttonPosition = m_LayoutCache.Calculate("btn", x => CalculateButtonPosition(layout));
                    buttonPosition = GlobalPoint(buttonPosition);

                    m_ThumbMouseOffset.X -= buttonPosition.X;
                    m_ThumbMouseOffset.Y -= buttonPosition.Y;
                    break;

                case UIMouseEventType.MouseUp:
                    m_ThumbDown = false;
                    break;
            }
        }

        public override void Update(UpdateState state)
        {
            base.Update(state);
            if (m_ThumbDown)
            {
                /** Dragging the thumb **/
                var mousePosition = this.GetMousePosition(state.MouseState);
                mousePosition.X -= m_ThumbMouseOffset.X;
                mousePosition.Y -= m_ThumbMouseOffset.Y;

                var layout = m_LayoutCache.Calculate(
                    "layout",
                    x => CalculateLayout()
                );

                float trackSize;
                float percent;
                if (Orientation == 0)
                { //horizontal
                    trackSize = m_Width - layout.ThumbFrom.Width;
                    percent = mousePosition.X / trackSize;
                }
                else
                { //vertical
                    trackSize = m_Height - layout.ThumbFrom.Height;
                    percent = mousePosition.Y / trackSize;
                }
                percent = Math.Min(Math.Max(0, percent), 1);

                var newValue = m_MinValue + ((m_MaxValue - m_MinValue) * percent);
                Value = newValue;
            }
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetSize(float width, float height)
        {
            m_Width = width;
            m_Height = height;
            m_LayoutCache.Invalidate();
        }

        private UISliderLayout CalculateLayout()
        {
            if (Orientation == 0)
            {   //horizontal
                var trackWidth = (int)(((float)m_Texture.Width) * 0.75);
                var thumbSize = m_Texture.Width - trackWidth;
                var oneThird = (int)(trackWidth / 3);

                return new UISliderLayout
                {
                    TrackStartFrom = new Rectangle(0, 0, oneThird, m_Texture.Height),
                    TrackMiddleFrom = new Rectangle(oneThird, 0, oneThird, m_Texture.Height),
                    TrackEndFrom = new Rectangle(trackWidth - oneThird, 0, oneThird, m_Texture.Height),

                    TrackStartTo = LocalPoint(Vector2.Zero),
                    TrackMiddleTo = LocalPoint(new Vector2(oneThird, 0)),
                    TrackEndTo = LocalPoint(new Vector2(m_Width - oneThird, 0)),

                    TrackMiddleScale = _Scale * new Vector2((m_Width - (oneThird * 2)) / oneThird, 1),

                    ThumbFrom = new Rectangle(m_Texture.Width - thumbSize, 0, thumbSize, m_Texture.Height)
                };
            }
            else
            {   //vertical
                var trackHeight = (int)(((float)m_Texture.Height) * 0.75);
                var thumbSize = m_Texture.Height - trackHeight;
                var oneThird = (int)(trackHeight / 3);

                return new UISliderLayout
                {
                    TrackStartFrom = new Rectangle(0, 0, m_Texture.Width, oneThird),
                    TrackMiddleFrom = new Rectangle(0, oneThird, m_Texture.Width, oneThird),
                    TrackEndFrom = new Rectangle(0, trackHeight - oneThird, m_Texture.Width, oneThird),

                    TrackStartTo = LocalPoint(Vector2.Zero),
                    TrackMiddleTo = LocalPoint(new Vector2(0, oneThird)),
                    TrackEndTo = LocalPoint(new Vector2(0, m_Height - oneThird)),

                    TrackMiddleScale = _Scale * new Vector2(1, (m_Height - (oneThird * 2)) / oneThird),

                    ThumbFrom = new Rectangle(0, m_Texture.Height - thumbSize, m_Texture.Width, thumbSize)
                };
            }
        }

        private Vector2 CalculateButtonPosition(UISliderLayout layout)
        {
            var size = m_MaxValue - m_MinValue;
            var value = (m_Value - m_MinValue) / size;

            Vector2 position;
            if (Orientation == 0)
            { //horizontal
                var majorPosition = (m_Width - layout.ThumbFrom.Width) * value;
                position = new Vector2(majorPosition, 0);
            }
            else
            { //vertical
                var majorPosition = (m_Height - layout.ThumbFrom.Height) * value;
                position = new Vector2(0, majorPosition);
            }

            /** Update mouse event info **/
            m_ThumbEvent.Region = new Rectangle((int)position.X, (int)position.Y, layout.ThumbFrom.Width, layout.ThumbFrom.Height);

            return LocalPoint(position);
        }


        public override void Draw(UISpriteBatch batch)
        {
            if (!Visible) { return; }

            var layout = m_LayoutCache.Calculate("layout", x => CalculateLayout());

            batch.Draw(m_Texture, layout.TrackStartTo, layout.TrackStartFrom, Color.White, 0, Vector2.Zero, _Scale, SpriteEffects.None, 0);
            batch.Draw(m_Texture, layout.TrackMiddleTo, layout.TrackMiddleFrom, Color.White, 0, Vector2.Zero, layout.TrackMiddleScale, SpriteEffects.None, 0);
            batch.Draw(m_Texture, layout.TrackEndTo, layout.TrackEndFrom, Color.White, 0, Vector2.Zero, _Scale, SpriteEffects.None, 0);

            if (m_MaxValue > m_MinValue)
            {
                var buttonPosition = m_LayoutCache.Calculate("btn", x => CalculateButtonPosition(layout));
                batch.Draw(m_Texture, buttonPosition, layout.ThumbFrom, Color.White, 0, Vector2.Zero, _Scale, SpriteEffects.None, 0);
            }
        }




        /**
         * Utility to add a +/- button to the slider
         */
        public UISliderButtonHandler AttachButtons(UIButton decrease, UIButton increase, int change)
        {
            return new UISliderButtonHandler(this, increase, decrease, change);
        }

    }


    public class UISliderButtonHandler
    {
        private UIButton increase;
        private UIButton decrease;
        private UISlider slider;

        public int Change;

        public UISliderButtonHandler(UISlider slider, UIButton increase, UIButton decrease, int change)
        {
            this.slider = slider;
            this.increase = increase;
            this.decrease = decrease;
            this.Change = change;

            increase.OnButtonClick += new ButtonClickDelegate(increase_OnButtonClick);
            decrease.OnButtonClick += new ButtonClickDelegate(decrease_OnButtonClick);

            slider.OnChange += new ChangeDelegate(slider_OnChange);
            slider.OnRangeChange += new ChangeDelegate(slider_OnRangeChange);

            Update();
        }

        void decrease_OnButtonClick(UIElement button)
        {
            slider.Value -= Change;
        }

        void increase_OnButtonClick(UIElement button)
        {
            slider.Value += Change;
        }


        private void Update()
        {
            var canScroll = slider.MaxValue > slider.MinValue;
            decrease.Disabled = !(canScroll && slider.Value > slider.MinValue);
            increase.Disabled = !(canScroll && slider.Value < slider.MaxValue);
        }


        void slider_OnChange(UIElement element)
        {
            Update();
        }

        void slider_OnRangeChange(UIElement element)
        {
            Update();
        }


    }



    public class UISliderLayout
    {
        public Rectangle TrackStartFrom;
        public Rectangle TrackMiddleFrom;
        public Rectangle TrackEndFrom;

        public Vector2 TrackStartTo;
        public Vector2 TrackMiddleTo;
        public Vector2 TrackEndTo;

        public Vector2 TrackMiddleScale;

        public Rectangle ThumbFrom;
    }
}
