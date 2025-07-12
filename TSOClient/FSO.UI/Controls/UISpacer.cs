using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;

namespace FSO.UI.Controls
{
    public class UISpacer : UIElement
    {
        public override Vector2 Size { get; set; }
        public UISpacer(int size)
        {
            Size = new Vector2(size);
        }

        public UISpacer(int width, int height)
        {
            Size = new Vector2(width, height);
        }

        public override void Draw(UISpriteBatch batch)
        {
        }
    }
}
