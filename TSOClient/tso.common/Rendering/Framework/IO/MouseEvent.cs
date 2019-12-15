/*
 * This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
 * If a copy of the MPL was not distributed with this file, You can obtain one at
 * http://mozilla.org/MPL/2.0/. 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
