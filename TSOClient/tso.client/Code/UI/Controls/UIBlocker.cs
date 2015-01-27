/*This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.

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
using TSOClient.Code.UI.Model;
using TSO.Common.rendering.framework.io;
using TSO.Common.rendering.framework.model;

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

        public override void Draw(UISpriteBatch batch)
        {
        }
    }
}
