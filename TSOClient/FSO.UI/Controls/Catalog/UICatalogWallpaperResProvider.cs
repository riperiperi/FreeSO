using Microsoft.Xna.Framework.Graphics;

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
