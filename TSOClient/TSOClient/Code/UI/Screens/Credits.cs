using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.UI.Framework;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Controls;

namespace TSOClient.Code.UI.Screens
{
    public class Credits : GameScreen
    {
        public Texture2D BackgroundImage { get; set; }

        public Credits()
        {
            this.RenderScript("credits.uis");
            this.AddAt(0, new UIImage(BackgroundImage));
        }
    }
}
