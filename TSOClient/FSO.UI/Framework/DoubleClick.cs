using FSO.Common.Rendering.Framework.IO;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;
using System;

namespace FSO.Client.UI.Framework
{
    public class DoubleClick
    {
        private const int MOUSE_DRIFT_TOLERANCE = 10;

        private long LastClick;
        private Point LastMousePosition;

        public bool TryDoubleClick(UIMouseEventType type, UpdateState update)
        {
            if(type == UIMouseEventType.MouseUp)
            {
                var now = (long)(DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds;
                if (now - LastClick < 500 && IsMouseClose(LastMousePosition, update.MouseState.Position))
                {
                    LastClick = now;
                    LastMousePosition = update.MouseState.Position;
                    return true;
                }
                LastClick = now;
                LastMousePosition = update.MouseState.Position;
            }

            return false;
        }

        private bool IsMouseClose(Point previous, Point current)
        {
            return Math.Abs(previous.X - current.X) < MOUSE_DRIFT_TOLERANCE &&
                    Math.Abs(previous.Y - current.Y) < MOUSE_DRIFT_TOLERANCE;
        }
    }
}
