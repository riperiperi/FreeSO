using FSO.Client.UI.Framework;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.UI.Panels.CityPainter.Previews
{
    internal class UICityPainterRoadsPreview : AbstractCityPainterPreview
    {
        private Texture2D[] RoadTilePreview;

        public override void Init(UICityPainter painter)
        {
            base.Init(painter);

            RoadTilePreview = LoadFSOTex([
                "roadcorner02.png",
                "roadcorner04.png",
                "road01.png",
                "road04.png",
                "roadcorner01.png",
                "roadcorner08.png",
            ]);
        }

        public override void Draw(UISpriteBatch batch)
        {
            // Just draw a road at the middle.

            var erasing = MapPainter.Erasing;
            var tint = erasing ? Color.White * 0.5f : Color.White;

            BeginTile(batch, new Point(3, 3));

            for (int i = 0; i < RoadTilePreview.Length; i++)
            {
                int x = i % 2;
                int y = i / 2;
                batch.Draw(RoadTilePreview[i], new Rectangle((x * 2) - 1, y * 2 - 2, 2, 2), tint);
            }

            EndTile(batch);

            if (MapPainter.Erasing)
            {
                PrepareTileMatrix(new Point(3, 3));

                var roadSize = 0.22f;

                var v1 = new Vector3(1 - roadSize, 0 - roadSize, 0);
                var v2 = new Vector3(1 + roadSize, 0 - roadSize, 0);
                var v3 = new Vector3(1 + roadSize, 2 + roadSize, 0);
                var v4 = new Vector3(1 - roadSize, 2 + roadSize, 0);

                Color color = Color.Red;

                DrawLine(batch, v1, v2, 2, color);
                DrawLine(batch, v2, v3, 2, color);
                DrawLine(batch, v3, v4, 2, color);
                DrawLine(batch, v4, v1, 2, color);
            }
        }
    }
}
