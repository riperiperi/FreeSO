using Microsoft.Xna.Framework.Graphics;
using FSO.Client.UI.Framework;

namespace FSO.Client.UI.Controls.Catalog
{
    public class UICatalogTerrainResProvider : UICatalogResProvider
    {
        public override string GetDescription(ulong id)
        {
            return GameFacade.Strings.GetString("f107", (id+101).ToString());
        }

        public override Texture2D GetIcon(ulong id)
        {
            switch (id)
            {
                case 0:
                    return UIElement.GetTexture(0x1E200000001); //up icon
                case 1:
                    return UIElement.GetTexture(0x1E100000001); //level icon
                case 2:
                    return UIElement.GetTexture(0x42A00000001); //wallpaper button icon (for grass)
                default:
                    return null;
            }
        }

        public override Texture2D GetThumb(ulong id)
        {
            switch (id)
            {
                case 0:
                    return UIElement.GetTexture(0x1D900000001); //up icon
                case 1:
                    return UIElement.GetTexture(0x1D800000001); //level icon
                case 2:
                    return UIElement.GetTexture(0x1D700000001); //down icon (used for grass....)
                default:
                    return null;
            }
        }

        public override string GetName(ulong id)
        {
            return GameFacade.Strings.GetString("f107", (id+1).ToString());
        }

        public override int GetPrice(ulong id)
        {
            return 1;
        }

        public override bool DoDispose()
        {
            return false;
        }
    }
}
