using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;

namespace FSO.Common.Rendering.Framework.IO
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
