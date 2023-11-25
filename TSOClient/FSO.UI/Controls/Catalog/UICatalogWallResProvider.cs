using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Controls.Catalog
{
    public class UICatalogWallResProvider : UICatalogResProvider
    {

        public override Texture2D GetIcon(ulong id)
        {
            return Content.Content.Get().WorldWalls.GetWallStyleIcon((ushort)id).GetTexture(GameFacade.GraphicsDevice);
        }

        public override string GetName(ulong id)
        {
            return Content.Content.Get().WorldWalls.GetWallStyle(id).Name;
        }

        public override string GetDescription(ulong id)
        {
            return Content.Content.Get().WorldWalls.GetWallStyle(id).Description;
        }

        public override int GetPrice(ulong id)
        {
            return Content.Content.Get().WorldWalls.GetWallStyle(id).Price;
        }
    }
}
