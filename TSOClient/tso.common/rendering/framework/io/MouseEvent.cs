using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Common.rendering.framework.model;
using Microsoft.Xna.Framework;

namespace TSO.Common.rendering.framework.io
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
        //public UIElement Element;
        public UIMouseEventType LastState;

        public IDepthProvider Element;
    }
}
