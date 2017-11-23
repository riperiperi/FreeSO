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
    public abstract class UICatalogResProvider
    {
        public abstract Texture2D GetIcon(ulong id);
        public abstract string GetName(ulong id);
        public abstract string GetDescription(ulong id);
        public abstract int GetPrice(ulong id);
        public virtual bool DoDispose()
        {
            return true;
        }

        public virtual Texture2D GetThumb(ulong id)
        {
            return GetIcon(id);
        }
    }
}
