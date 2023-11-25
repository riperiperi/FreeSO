using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Controls.Catalog
{
    public abstract class UICatalogResProvider
    {
        public abstract Texture2D GetIcon(ulong id);
        public abstract string GetName(ulong id);
        public abstract string GetDescription(ulong id);
        public abstract int GetPrice(ulong id);
        public virtual bool DoDispose()
        {
            return true;
        }

        public virtual Texture2D GetThumb(ulong id)
        {
            return GetIcon(id);
        }
    }
}
