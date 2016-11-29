using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Controls.Catalog
{
    public class UICatalogRoofResProvider : UICatalogResProvider
    {
        public Texture2D GetIcon(ulong id)
        {
            var roofs = Content.Content.Get().WorldRoofs;
            return roofs.Get(roofs.IDToName((int)id)).Get(GameFacade.GraphicsDevice);
        }

        public bool DisposeIcon(ulong id)
        {
            return false;
        }

        public string GetName(ulong id)
        {
            return "";
        }

        public string GetDescription(ulong id)
        {
            return "";
        }

        public int GetPrice(ulong id)
        {
            return 0;
        }
    }
}
