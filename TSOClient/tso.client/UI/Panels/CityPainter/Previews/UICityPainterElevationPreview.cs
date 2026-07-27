using FSO.Client.Rendering.City.Plugins.PainterModes;
using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;

namespace FSO.Client.UI.Panels.CityPainter.Previews
{

    internal class UICityPainterElevationPreview : AbstractCityPainterPreview
    {
        public MapPainterSpraypaint Spray;

        public override void Init(UICityPainter painter)
        {
            Spray = new MapPainterSpraypaint(true);

            base.Init(painter);
        }

        public override void Draw(UISpriteBatch batch)
        {
            // Draw a grid representing the elevation change

            int tileCount = MapPainter.BrushSize * 2 + 2;
            int vertCount = tileCount + 1;
            PrepareTileMatrix(new Point(tileCount));

            bool[] tileTouched = new bool[tileCount * tileCount];
            float[] vertices = new float[vertCount * vertCount];
            float[] intensityVertices = vertices;
            int center = vertCount / 2;

            float baseSize = MapPainter.BrushSize + 0.5f;
            var multiplier = MathF.Pow(baseSize, 0.8f) * ((MapPainter.Accelerate) ? 8 : 4);

            var erasing = !MapPainter.Flatten && MapPainter.Erasing;

            if (erasing)
            {
                multiplier *= -1;
            }

            float intensity = 1;
            if (MapPainter.Flatten)
            {
                int vi = 0;
                for (int y = 0; y < vertCount; y++)
                {
                    for (int x = 0; x < vertCount; x++)
                    {
                        vertices[vi++] = (y - vertCount / 2f) * -0.5f;
                    }
                }

                multiplier = 50;
                intensityVertices = new float[vertCount * vertCount];
                var centerElev = 0;

                IMapPainterMode.BrushFunc(MapPainter.BrushSize, (x, y, strength) =>
                {
                    if (strength > 0)
                    {
                        int vertInd = (y + center) * vertCount + x + center;
                        var elev = vertices[vertInd];

                        var change = (centerElev - elev) / 50f * multiplier;
                        if (change > 0) change = Math.Max(0.02f, change);
                        else change = Math.Min(-0.02f, change);

                        vertices[vertInd] += change;
                        intensityVertices[vertInd] = Math.Max(Math.Abs(change), strength);
                    }
                });
            }
            else
            {
                intensity = MapPainter.BrushIntensity;
                multiplier *= intensity;
                IMapPainterMode.BrushFunc(MapPainter.BrushSize, (x, y, strength) =>
                {
                    if (strength > 0)
                    {
                        if (MapPainter.RoughTerrain)
                        {
                            strength = Spray.GetRoughEdge((256 + y) * 512 + 256 + x, strength, MapPainter.BrushSize);
                        }

                        vertices[(y + center) * vertCount + x + center] = strength * multiplier;
                    }
                });
            }

            Vector3 offset = new Vector3(-baseSize, -baseSize, 0);
            Color baseColor = erasing ? Color.Red : Color.White;

            for (int y = 0; y < tileCount; y++)
            {
                for (int x = 0; x < tileCount; x++)
                {
                    Vector3 v1 = new Vector3(x, y, vertices[y * vertCount + x]) + offset;
                    Vector3 v2 = new Vector3(x + 1, y, vertices[y * vertCount + x + 1]) + offset;
                    Vector3 v3 = new Vector3(x, y + 1, vertices[(y + 1) * vertCount + x]) + offset;
                    Vector3 v4 = new Vector3(x + 1, y + 1, vertices[(y + 1) * vertCount + x + 1]) + offset;

                    float e1 = intensityVertices[y * vertCount + x];
                    float e2 = intensityVertices[y * vertCount + x + 1];
                    float e3 = intensityVertices[(y + 1) * vertCount + x];
                    float e4 = intensityVertices[(y + 1) * vertCount + x + 1];

                    float mag = (e1 + e2 + e3 + e4) / 4;

                    Color color = baseColor * Math.Min(1f, Math.Abs(mag / multiplier));

                    DrawLine(batch, v1, v2, 2, color);
                    DrawLine(batch, v3, v4, 2, color);
                    DrawLine(batch, v1, v3, 2, color);
                    DrawLine(batch, v2, v4, 2, color);
                }
            }
        }
    }
}
