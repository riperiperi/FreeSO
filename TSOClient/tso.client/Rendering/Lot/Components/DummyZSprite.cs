using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TSOClient.Code.Rendering.Lot.Model;
using Microsoft.Xna.Framework.Graphics;
using TSOClient.Code.Rendering.Lot.Framework;

namespace TSOClient.Code.Rendering.Lot.Components
{
    public class DummyZSprite : House2DComponent
    {
        private HouseBatchSprite Sprite;


        public DummyZSprite(string prefix)
        {
            var gd = GameFacade.GraphicsDevice;

            var alpha = Texture2D.FromFile(gd, prefix + "a.png");
            var pixel = Texture2D.FromFile(gd, prefix + "p.png");
            var depth = Texture2D.FromFile(gd, prefix + "z.png");

            var tex = new Texture2D(gd, pixel.Width, pixel.Height);
            var texData = new Color[pixel.Width * pixel.Height];
            var alphaData = new Color[pixel.Width * pixel.Height];
            var pixelData = new Color[pixel.Width * pixel.Height];


            pixel.GetData(pixelData);
            alpha.GetData(alphaData);

            for (var i = 0; i < texData.Length; i++)
            {
                var pixelPx = pixelData[i];
                var alphaPx = alphaData[i];
                pixelPx.A = alphaPx.R;

                texData[i] = pixelPx;
            }

            tex.SetData(texData);
            pixel.Dispose();
            alpha.Dispose();


            Sprite = new HouseBatchSprite
            {
                Pixel = tex,
                RenderMode = HouseBatchRenderMode.Z_BUFFER,
                Depth = depth,
                SrcRect = new Microsoft.Xna.Framework.Rectangle(0, 0, pixel.Width, pixel.Height),
                DestRect = new Microsoft.Xna.Framework.Rectangle(0, 0, pixel.Width, pixel.Height)
            };
        }



        public override int Height
        {
            get { return 0;  }
        }

        public override void Draw(HouseRenderState state, HouseBatch batch)
        {
            //ZBuffer

            batch.Draw(Sprite);
            //batch.DrawZ(Texture, ZBuffer, new Microsoft.Xna.Framework.Rectangle(0, 0, Texture.Width, Texture.Height), Color.White);
        }
    }
}
