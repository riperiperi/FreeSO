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

namespace FSO.Client.UI.Panels.LotControls
{
    public interface UICustomLotControl
    {
        void MouseDown(UpdateState state);
        void MouseUp(UpdateState state);
        void Update(UpdateState state, bool scrolled);

        void Release();
    }
}
