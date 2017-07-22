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
        public override Texture2D GetIcon(ulong id)
        {
            var roofs = Content.Content.Get().WorldRoofs;
            return roofs.Get(roofs.IDToName((int)id)).Get(GameFacade.GraphicsDevice);
        }

        public bool DisposeIcon(ulong id)
        {
            return false;
        }

        public override string GetName(ulong id)
        {
            return "";
        }

        public override string GetDescription(ulong id)
        {
            return "";
        }

        public override int GetPrice(ulong id)
        {
            return 0;
        }
    }
}
