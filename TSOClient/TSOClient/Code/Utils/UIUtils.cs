using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Model;
using System.Drawing;

namespace TSOClient.Code.Utils
{
    public class UIUtils
    {
        public static UIDragHandler MakeDraggable(UIElement mouseTarget, UIElement dragControl)
        {
            var handler = new UIDragHandler(mouseTarget, dragControl);
            return handler;
        }


    }


    public class UIDragHandler
    {
        public UIElement MouseTarget;
        public UIElement DragControl;
        public UIMouseEventRef MouseEvent;

        private UpdateHookDelegate UpdateHook;

        public UIDragHandler(UIElement mouseTarget, UIElement dragControl)
        {
            UpdateHook = new UpdateHookDelegate(Update);

            MouseTarget = mouseTarget;
            DragControl = dragControl;
            MouseEvent = mouseTarget.ListenForMouse(mouseTarget.GetBounds(), new UIMouseEvent(DragMouseEvents));
            
        }

        private bool m_doDrag = false;
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
                    DragControl.AddUpdateHook(UpdateHook);

                    var position = DragControl.GetMousePosition(state.MouseState);
                    m_dragOffsetX = position.X;
                    m_dragOffsetY = position.Y;
                    break;

                case UIMouseEventType.MouseUp:
                    /** Stop drag **/
                    m_doDrag = false;
                    DragControl.RemoveUpdateHook(UpdateHook);
                    break;
            }
        }

        private void Update(UpdateState state)
        {
            if (m_doDrag)
            {
                /** Drag the dialog box **/
                var position = DragControl.Parent.GetMousePosition(state.MouseState);
                DragControl.X = position.X - m_dragOffsetX;
                DragControl.Y = position.Y - m_dragOffsetY;
            }
        }




    }
}
