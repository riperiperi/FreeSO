/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FSO.Client.UI.Framework;

namespace FSO.Client.UI.Panels
{
    public abstract class UIDestroyablePanel : UIContainer
    {
        //just a panel with a destroy function, so that any hooks can be detached.
        public abstract void Destroy();
    }
}
