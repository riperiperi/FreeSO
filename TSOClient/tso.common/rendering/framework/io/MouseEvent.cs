using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using tso.common.rendering.framework.model;
using Microsoft.Xna.Framework;

namespace tso.common.rendering.framework.io
{
    public enum UIMouseEventType
    {
        MouseOver,
        MouseOut,
        MouseDown,
        MouseUp
    }

    public delegate void UIMouseEvent(UIMouseEventType type, UpdateState state);

    public class UIMouseEventRef
    {
        public UIMouseEvent Callback;
        public Rectangle Region;
        /** Uwed to work out who got the mouse event when two components overlap **/
        public float Depth;
        //public UIElement Element;
        public UIMouseEventType LastState;
    }
}
