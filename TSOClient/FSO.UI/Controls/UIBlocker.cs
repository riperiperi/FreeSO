using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// Just blocks and sinks mouse events
    /// </summary>
    public class UIBlocker : UIElement
    {
        private UIMouseEventRef MouseEvt;
        public UIMouseEvent OnMouseEvt;

        public UIBlocker()
        {
            MouseEvt = this.ListenForMouse(new Microsoft.Xna.Framework.Rectangle(0, 0, 10, 10), OnMouse);
            SetSize(GlobalSettings.Default.GraphicsWidth, GlobalSettings.Default.GraphicsHeight);
        }

        public UIBlocker(int width, int height)
        {
            MouseEvt = this.ListenForMouse(new Microsoft.Xna.Framework.Rectangle(0, 0, 10, 10), OnMouse);
            SetSize(width, height);
        }

        private void OnMouse(UIMouseEventType type, UpdateState state)
        {
            if (OnMouseEvt != null) OnMouseEvt(type, state);
        }

        public void SetSize(int width, int height)
        {
            MouseEvt.Region.Width = width;
            MouseEvt.Region.Height = height;
        }

        public override void Draw(UISpriteBatch batch)
        {
        }
    }
}
