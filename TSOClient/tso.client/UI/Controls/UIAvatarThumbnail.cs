using FSO.Client.UI.Framework;
using FSO.Common.Rendering.Framework.IO;
using FSO.Content.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FSO.Client.UI.Controls
{
    public class UIAvatarThumbnail : UIElement
    {
        public ITextureRef Icon;
        private ITextureRef Background;
        private UIMouseEventRef ClickHandler;

        public UIAvatarThumbnail()
        {

        }

        public override void Draw(UISpriteBatch batch)
        {

        }
    }
}
