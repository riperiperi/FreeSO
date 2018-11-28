/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework;
using FSO.Client.UI.Model;
using FSO.Client.UI.Framework.Parser;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using FSO.Client.GameContent;

namespace FSO.Client.UI.Controls
{
    [Flags]
    public enum UIDialogStyle
    {
        Standard = 0,
        Tall = 1,
        OK = 2,
        Close = 4
    }

    [Flags]
    public enum UIDialogExtras
    {
        None,
        CloseButton,
        AcceptButton
    }

    /// <summary>
    /// Generic dialog component
    /// </summary>
    public class UIDialog : UICachedContainer
    {
        protected UIImage Background;
        public string Caption { get; set; }
        public TextStyle CaptionStyle = TextStyle.DefaultTitle;
        public Rectangle CaptionMargin = new Rectangle(0, 3, 0, 0);

        //if dialog type does not specify these, they do not exist
        public UIImage CloseBg;
        private UIImage OKBg;
        public UIButton OKButton;
        public UIButton CloseButton;

        //Tolerance for how far out of the screen controls can be dragged.
        protected static int m_DragTolerance = 20;
        protected UIButton AcceptButton;

        public UIDialog(UIDialogStyle style, bool draggable) : this(style, UIDialogExtras.None, draggable)
        {
        }

        public UIDialog(UIDialogStyle style, UIDialogExtras extras, bool draggable)
        {
            if ((style & UIDialogStyle.Tall) > 0)
            {
                Background = new UIImage(GetTexture((ulong)FileIDs.UIFileIDs.dialog_backgroundtemplatetall))
                .With9Slice(41, 41, 66, 40);
            }
            else
            {
                var tx = GetTexture((ulong)FileIDs.UIFileIDs.dialog_backgroundtemplate);
                Background = new UIImage(tx)
                            .With9Slice(41, 41, 60, 40);
            }

            Background.ID = "Background";

            /** Drag area **/
            if (draggable)
            {
                Background.ListenForMouse(new UIMouseEvent(DragMouseEvents));
            }

            this.Add(Background);

            if ((style & UIDialogStyle.OK) > 0)
            {
                OKBg = new UIImage(GetTexture((ulong)1670742278146));
                OKButton = new UIButton(GetTexture((ulong)9423158247425));
                Add(OKBg);
                Add(OKButton);
            }

            if ((style & UIDialogStyle.Close) > 0)
            {
                CloseBg = new UIImage(GetTexture(((style & UIDialogStyle.Tall) > 0) ?
                    ((ulong)1481763717122L) :
                    ((ulong)1477468749826L)
                    ));
                CloseButton = new UIButton(GetTexture((ulong)8697308774401L));
                Add(CloseBg);
                Add(CloseButton);
            }
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

        public override void Update(UpdateState state)
        {
            base.Update(state);

            if (m_doDrag)
            {
                /** Drag the dialog box **/
                var position = Parent.GetMousePosition(state.MouseState);
                
                if((position.X - m_dragOffsetX) < (GlobalSettings.Default.GraphicsWidth - m_DragTolerance) && (position.X - m_dragOffsetX) > 0)
                    this.X = position.X - m_dragOffsetX;
                if ((position.Y - m_dragOffsetY) < (GlobalSettings.Default.GraphicsHeight - m_DragTolerance) && (position.Y - m_dragOffsetY) > 0)
                    this.Y = position.Y - m_dragOffsetY;
            }
        }

        public override void Draw(UISpriteBatch batch)
        {
            base.Draw(batch);

            if (Visible && Caption != null && CaptionStyle != null)
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

            if (OKBg != null)
            {
                OKBg.Position = new Vector2(width - 53, height - 46);
                OKButton.Position = OKBg.Position + new Vector2(10, 4);
            }

            if (CloseBg != null)
            {
                CloseBg.Position = new Vector2(width - 70, 0);
                CloseButton.Position = CloseBg.Position + new Vector2(45, 10);
            }

            m_Bounds = new Rectangle(0, 0, width, height);
            Size = new Vector2(width, height);
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
