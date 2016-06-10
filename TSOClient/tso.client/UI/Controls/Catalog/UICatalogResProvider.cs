/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FSO.Client.UI.Controls.Catalog
{
    public interface UICatalogResProvider
    {
        Texture2D GetIcon(ulong id);
        string GetName(ulong id);
        string GetDescription(ulong id);
        int GetPrice(ulong id);
    }
}
