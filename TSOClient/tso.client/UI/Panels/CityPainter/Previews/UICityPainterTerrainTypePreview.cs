using FSO.Client.Rendering.City.Plugins.PainterModes;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Panels.CityPainter.Previews
{

    internal class UICityPainterTerrainTypePreview : AbstractCityPainterPreview
    {
        public Texture2D[] TerrainTextures;
        public MapPainterSpraypaint Spray;
        public override void Init(UICityPainter painter)
        {
            base.Init(painter);

            TerrainTextures = LoadTSOTex([
                "terrain/newformat/gr.tga",
                "terrain/newformat/wt.tga",
                "terrain/newformat/rk.tga",
                "terrain/newformat/sn.tga",
                "terrain/newformat/sd.tga",
                ]);

            Spray = new MapPainterSpraypaint(true);
        }

        private int PosMod(int x, int m)
        {
            return (x % m + m) % m;
        }

        public override void Draw(UISpriteBatch batch)
        {
            // Draw the terrain brush result

            var size = MapPainter.BrushSize;

            var tex = TerrainTextures[MapPainter.SelectedModifier];
            var texSegment = new Point(tex.Width / 4, tex.Height / 4);
            var spray = MapPainter.SprayBrush;
            var intensity = MapPainter.BrushIntensity * 0.8f + 0.2f; // Small bias to assist the display.

            BeginTile(batch, new Point(size * 2 + 1));
            IMapPainterMode.BrushFunc(size, (x, y, strength) =>
            {
                var multiplier = (MapPainter.Accelerate) ? 2 : 1;

                if (spray)
                {
                    var brushIntensity = Spray.GetSpraypaint((256 + y) * 512 + 256 + x, strength) * intensity * multiplier;
                    strength = brushIntensity - 0.3f;
                }

                if (strength > 0)
                {
                    batch.Draw(tex, new Rectangle(x, y, 1, 1), new Rectangle(PosMod(x, 4) * texSegment.X, PosMod(y, 4) * texSegment.Y, texSegment.X, texSegment.Y), Color.White);
                }
            });

            EndTile(batch);
        }
    }
}
