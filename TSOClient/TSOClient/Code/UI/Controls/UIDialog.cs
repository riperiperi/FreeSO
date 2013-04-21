using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using TSOClient.Code.UI.Model;
using TSOClient.Code.UI.Framework.Parser;

namespace TSOClient.Code.UI.Controls
{
    public enum UIDialogStyle
    {
        Standard,
        StandardTall
    }

    /// <summary>
    /// Generic dialog component
    /// </summary>
    public class UIDialog : UIContainer
    {
        private UIImage Background;
        public string Caption { get; set; }
        public TextStyle CaptionStyle = TextStyle.DefaultTitle;
        public Rectangle CaptionMargin = new Rectangle(0, 3, 0, 0);

        public UIDialog(UIDialogStyle style, bool draggable)
        {
            int dragHeight = 0;

            switch (style)
            {
                case UIDialogStyle.Standard:
                    var tx = GetTexture(0xE500000002);
                    Background = new UIImage(tx)
                                    .With9Slice(41, 41, 60, 40);
                    break;

                case UIDialogStyle.StandardTall:
                    Background = new UIImage(GetTexture(0x15700000002))
                                    .With9Slice(41, 41, 66, 40);
                    break;
            }

            Background.ID = "Background";

            /** Drag area **/
            if (draggable)
            {
                Background.ListenForMouse(new UIMouseEvent(DragMouseEvents));
            }

            this.Add(Background);
        }

        public void CenterAround(UIElement element)
        {
            CenterAround(element, 0, 0);
        }

        public void CenterAround(UIElement element, int offsetX, int offsetY)
        {
            var bounds = element.GetBounds();
            if (bounds == null) { return; }


            var topLeft =
                element.LocalPoint(new Microsoft.Xna.Framework.Vector2(bounds.X, bounds.Y));


            this.X = offsetX + topLeft.X + ((bounds.Width - this.Width) / 2);
            this.Y = offsetY + topLeft.Y + ((bounds.Height - this.Height) / 2);
        }


        private bool m_doDrag;
        private float m_dragOffsetX;
        private float m_dragOffsetY;

        /// <summary>
        /// Handle mouse events for dragging
        /// </summary>
        /// <param name="evt"></param>
        private void DragMouseEvents(UIMouseEventType evt, UpdateState state)
        {
            switch (evt)
            {
                case UIMouseEventType.MouseDown:
                    /** Start drag **/
                    m_doDrag = true;
                    var position = this.GetMousePosition(state.MouseState);
                    m_dragOffsetX = position.X;
                    m_dragOffsetY = position.Y;
                    break;

                case UIMouseEventType.MouseUp:
                    /** Stop drag **/
                    m_doDrag = false;
                    break;
            }
        }


        public override void Update(TSOClient.Code.UI.Model.UpdateState state)
        {
            base.Update(state);

            if (m_doDrag)
            {
                /** Drag the dialog box **/
                var position = Parent.GetMousePosition(state.MouseState);
                this.X = position.X - m_dragOffsetX;
                this.Y = position.Y - m_dragOffsetY;
            }
        }


        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);

            if (Caption != null && CaptionStyle != null)
            {
                DrawLocalString(batch, Caption, Vector2.Zero, CaptionStyle, GetBounds(), TextAlignment.Top | TextAlignment.Center, CaptionMargin);
            }
        }

        /// <summary>
        /// Set the size of the dialog
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void SetSize(int width, int height)
        {
            Background.SetSize(width, height);
            m_Bounds = new Rectangle(0, 0, width, height);
        }

        private Rectangle m_Bounds;
        public override Rectangle GetBounds()
        {
            return m_Bounds;
        }

        public int Width { get { return m_Bounds.Width; } }
        public int Height { get { return m_Bounds.Height; } }

        [UIAttribute("size")]
        public Point DialogSize
        {
            get
            {
                return new Point(m_Bounds.Width, m_Bounds.Height);
            }
            set
            {
                SetSize(value.X, value.Y);
            }
        }
    }
}
