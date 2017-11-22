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
using FSO.Content;
using FSO.Files.Formats.IFF.Chunks;

namespace FSO.Client.UI.Controls.Catalog
{
    public class UICatalogWallpaperResProvider : UICatalogResProvider
    {
        public override Texture2D GetIcon(ulong id)
        {
            return Content.Content.Get().WorldWalls.GetWallThumb((ushort)id, GameFacade.GraphicsDevice);
        }

        public bool DisposeIcon(ulong id)
        {
            return (id > 255);
        }

        public override string GetName(ulong id)
        {
            return Content.Content.Get().WorldWalls.Entries[(ushort)id].Name;
        }

        public override string GetDescription(ulong id)
        {
            return Content.Content.Get().WorldWalls.Entries[(ushort)id].Description;
        }

        public override int GetPrice(ulong id)
        {
            return Content.Content.Get().WorldWalls.Entries[(ushort)id].Price;
        }

        public override bool DoDispose()
        {
            return false;
        }
    }
}
