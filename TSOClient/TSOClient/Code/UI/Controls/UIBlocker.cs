using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using TSOClient.Code.UI.Model;

namespace TSOClient.Code.UI.Controls
{
    /// <summary>
    /// Just blocks and sinks mouse events
    /// </summary>
    public class UIBlocker : UIElement
    {
        private UIMouseEventRef MouseEvt;

        public UIBlocker()
        {
            MouseEvt = this.ListenForMouse(new Microsoft.Xna.Framework.Rectangle(0, 0, 10, 10), OnMouse);
            SetSize(GlobalSettings.Default.GraphicsWidth, GlobalSettings.Default.GraphicsHeight);
        }

        private void OnMouse(UIMouseEventType type, UpdateState state)
        {
        }


        public void SetSize(int width, int height)
        {
            MouseEvt.Region.Width = width;
            MouseEvt.Region.Height = height;
        }

        public override void Draw(Microsoft.Xna.Framework.Graphics.SpriteBatch batch)
        {
        }
    }
}
