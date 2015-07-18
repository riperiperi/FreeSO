using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSO.Content;
using TSO.Files.formats.iff.chunks;

namespace TSOClient.Code.UI.Controls.Catalog
{
    public class UICatalogWallpaperResProvider : UICatalogResProvider
    {
        public Texture2D GetIcon(ulong id)
        {
            BMP bitmap = Content.Get().WorldWalls.GetWallIcon((ushort)id);
            return (bitmap == null)?null:bitmap.GetTexture(GameFacade.GraphicsDevice);
        }
    }
}
