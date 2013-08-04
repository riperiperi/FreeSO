/*The contents of this file are subject to the Mozilla Public License Version 1.1
(the "License"); you may not use this file except in compliance with the
License. You may obtain a copy of the License at http://www.mozilla.org/MPL/

Software distributed under the License is distributed on an "AS IS" basis,
WITHOUT WARRANTY OF ANY KIND, either express or implied. See the License for
the specific language governing rights and limitations under the License.

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
