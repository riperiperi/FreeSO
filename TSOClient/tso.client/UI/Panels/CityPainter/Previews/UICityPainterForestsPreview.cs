using FSO.Client.Rendering.City.Plugins.PainterModes;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Panels.CityPainter.Previews
{
    internal class UICityPainterForestsPreview : AbstractCityPainterPreview
    {
        private Texture2D Forests;
        public MapPainterSpraypaint Spray;

        public override void Init(UICityPainter painter)
        {
            base.Init(painter);

            Forests = LoadTSOTex("farzoom/forest00a.tga");
            Spray = new MapPainterSpraypaint(true);
        }

        public override void Draw(UISpriteBatch batch)
        {
            // Draw forests based on the brush and the type
            // If it's erasing then draw a red grid under them

            var fw = Forests.Width / 4;
            var fh = Forests.Height / 4;
            float intensityS = MapPainter.BrushIntensity;
            float intensityF = MapPainter.BrushIntensity - 1;
            int intensity = Math.Clamp((int)MathF.Round(intensityF), 0, 3);
            int type = MapPainter.SelectedModifier;

            var size = MapPainter.BrushSize;

            Color tint = Color.White;

            PrepareTileMatrix(new Point(size * 2 + 1));

            if (MapPainter.Erasing)
            {
                tint *= 0.5f;

                IMapPainterMode.BrushFunc(size, (x, y, strength) =>
                {
                    var multiplier = (MapPainter.Accelerate) ? 2 : 1;
                    if (strength > 0)
                    {
                        var v1 = new Vector3(x, y, 0);
                        var v2 = new Vector3(x + 1, y, 0);
                        var v3 = new Vector3(x + 1, y + 1, 0);
                        var v4 = new Vector3(x, y + 1, 0);

                        Color color = Color.Red * Math.Min(1f, strength + 0.5f);

                        DrawLine(batch, v1, v2, 2, color);
                        DrawLine(batch, v2, v3, 2, color);
                        DrawLine(batch, v3, v4, 2, color);
                        DrawLine(batch, v4, v1, 2, color);
                    }
                });
            }

            var spray = MapPainter.SprayBrush;

            IMapPainterMode.BrushFunc(size, (x, y, strength) =>
            {
                var multiplier = (MapPainter.Accelerate) ? 2 : 1;

                if (spray)
                {
                    var brushIntensity = Spray.GetSpraypaint((256 + y) * 512 + 256 + x, strength) * intensityS * multiplier;
                    intensity = Math.Clamp((int)MathF.Round(brushIntensity * 8), 0, 4) - 1;

                    if (intensity >= 0)
                    {
                        var src = new Rectangle(intensity * fw, type * fh, fw, fh);
                        var dst = GetTilePosition(x, y, fw, fh);

                        DrawLocalTexture(batch, Forests, src, dst.Item1, dst.Item2, tint);
                    }
                }
                else
                {
                    if (strength > 0)
                    {
                        var src = new Rectangle(intensity * fw, type * fh, fw, fh);
                        var dst = GetTilePosition(x, y, fw, fh);

                        DrawLocalTexture(batch, Forests, src, dst.Item1, dst.Item2, tint);
                    }
                }
            });
        }
    }
}
