using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.UI.Framework;

namespace TSOClient.Code.UI.Controls
{
    public class UIRectangle : UIElement
    {
        public override void Draw(SpriteBatch batch)
        {
            var whiteRectangle = new Texture2D(batch.GraphicsDevice, 1, 1);
            whiteRectangle.SetData(new[] { Color.White });

            var pos = LocalRect(0, 0, 50, 50);
            batch.Draw(whiteRectangle, pos, Color.White);
        }
    }
}
