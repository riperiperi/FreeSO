using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content;
using TSO.Files.formats.iff.chunks;

namespace TSOClient.Code.UI.Controls.Catalog
{
    public class UICatalogFloorResProvider : UICatalogResProvider
    {
        public Texture2D GetIcon(ulong id)
        {
            return Content.Get().WorldFloors.GetFloorThumb((ushort)id, GameFacade.GraphicsDevice);
        }

        public bool DisposeIcon(ulong id)
        {
            return (id > 255);
        }
    }
}
