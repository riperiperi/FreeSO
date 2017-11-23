/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Common.Rendering.Framework.Model;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.LotControls
{
    public abstract class UICustomLotControl
    {
        public UILotControlModifiers Modifiers { get; set; }
        public Point MousePosition { get; set; }

        public abstract void MouseDown(UpdateState state);
        public abstract void MouseUp(UpdateState state);
        public abstract void Update(UpdateState state, bool scrolled);

        public abstract void Release();
    }

    [Flags]
    public enum UILotControlModifiers
    {
        SHIFT = 1,
        CTRL = 2
    }
}
