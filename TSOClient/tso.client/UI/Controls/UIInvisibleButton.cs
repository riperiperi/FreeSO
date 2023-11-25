using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using FSO.Common.Rendering.Framework.IO;

namespace FSO.Client.UI.Controls
{
    /// <summary>
    /// A simple invisible rectangular button.
    /// </summary>
    public class UIInvisibleButton : UIButton
    {
        public UIInvisibleButton(int width, int height, Texture2D invisibleTexture)
        {
            Texture = invisibleTexture;

            ImageStates = 1;

            ClickHandler =
                ListenForMouse(new Rectangle(0, 0, width, height), new UIMouseEvent(OnMouseEvent));

            ActivateTooltip();
        }
    }
}
