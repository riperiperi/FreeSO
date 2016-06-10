/*
This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0.
If a copy of the MPL was not distributed with this file, You can obtain one at
http://mozilla.org/MPL/2.0/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using FSO.Content;

namespace FSO.Client.UI.Controls.Catalog
{
    public class UICatalogWallResProvider : UICatalogResProvider
    {

        public Texture2D GetIcon(ulong id)
        {
            return Content.Content.Get().WorldWalls.GetWallStyleIcon((ushort)id).GetTexture(GameFacade.GraphicsDevice);
        }

        public string GetName(ulong id)
        {
            return Content.Content.Get().WorldWalls.GetWallStyle(id).Name;
        }

        public string GetDescription(ulong id)
        {
            return Content.Content.Get().WorldWalls.GetWallStyle(id).Description;
        }

        public int GetPrice(ulong id)
        {
            return Content.Content.Get().WorldWalls.GetWallStyle(id).Price;
        }
    }
}
