/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Model;
using FSO.Common.Rendering.Framework.Model;

namespace FSO.Client.UI.Framework
{
    /// <summary>
    /// Non-visual UI component. For example, an animation library that needs to be involved
    /// with the update loop
    /// </summary>
    public interface IUIProcess
    {
        void Update(UpdateState state);
    }
}
