using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSO.Content;

namespace TSOClient.Code.UI.Controls.Catalog
{
    public class UICatalogWallResProvider : UICatalogResProvider
    {
        public Texture2D GetIcon(ulong id)
        {
            return Content.Get().WorldWalls.GetWallStyleIcon((ushort)id).GetTexture(GameFacade.GraphicsDevice);
        }
    }
}
