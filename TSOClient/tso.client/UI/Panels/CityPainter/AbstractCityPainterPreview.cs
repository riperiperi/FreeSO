using FSO.Client.Rendering.City.Plugins;
using FSO.Client.UI.Framework;
using FSO.Common;
using FSO.Common.Utils;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using XnaMatrix = Microsoft.Xna.Framework.Matrix;

namespace FSO.Client.UI.Panels.CityPainter
{
    internal abstract class AbstractCityPainterPreview : UIElement
    {
        private Vector2 _Size;
        public override Vector2 Size { get => _Size; set => _Size = value; }

        protected UICityPainter Painter { get; private set; }
        protected MapPainterPlugin MapPainter => Painter.MapPainter;

        private XnaMatrix TileMatrix;
        private float TileScale;

        public AbstractCityPainterPreview()
        {
        }

        public virtual void Init(UICityPainter painter)
        {
            Painter = painter;
        }

        protected Texture2D LoadFSOTex(string name)
        {
            string path = Path.Combine(FSOEnvironment.ContentDir, "Textures/terrain/", name);

            return TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, path);
        }

        protected Texture2D[] LoadFSOTex(ReadOnlySpan<string> names)
        {
            var result = new Texture2D[names.Length];

            for (int i = 0; i < names.Length; i++)
            {
                result[i] = LoadFSOTex(names[i]);
            }

            return result;
        }

        protected Texture2D LoadTSOTex(string path)
        {
            string gamepath = GameFacade.GameFilePath($"gamedata/{path}");

            return TextureUtils.TextureFromFile(GameFacade.GraphicsDevice, gamepath);
        }

        protected Texture2D[] LoadTSOTex(ReadOnlySpan<string> paths)
        {
            var result = new Texture2D[paths.Length];

            for (int i = 0; i < paths.Length; i++)
            {
                result[i] = LoadTSOTex(paths[i]);
            }

            return result;
        }

        private (XnaMatrix, float) GetTileSpaceMatrix(Point size)
        {
            var diagSize = (size.X + size.Y) / 2f;
            float diag = MathF.Sqrt(2);

            float scale = 1 / diagSize;

            float tileWidth = scale * 128 / diag;

            return (
                XnaMatrix.CreateTranslation(new Vector3(-0.5f, -0.5f, 0)) *
                XnaMatrix.CreateRotationZ(MathF.PI / 4f) *
                XnaMatrix.CreateScale(new Vector3(tileWidth, tileWidth / 2, 1)),
                scale);
        }

        protected void PrepareTileMatrix(Point size)
        {
            (TileMatrix, TileScale) = GetTileSpaceMatrix(size);
        }

        protected (Vector2, Vector2) GetTilePosition(int x, int y, int width, int height)
        {
            var ctr = Vector2.Transform(new Vector2(x + 0.5f, y + 0.5f), TileMatrix) + (Size / 2f);
            var scale = TileScale;

            return (ctr - new Vector2(width / 2, height - 32) * scale, new Vector2(scale, scale));
        }

        protected void BeginTile(UISpriteBatch batch, Point size)
        {
            batch.Pause();
            // Calculate a new matrix in tile space starting at the center of this component.

            var trueScale = Scale;
            var trueCenter = LocalPoint(Size.X / 2f, Size.Y / 2f) / Scale;

            var toCenter = XnaMatrix.CreateTranslation(new Vector3(trueCenter, 0)) * XnaMatrix.CreateScale(new Vector3(trueScale, 1));

            var mat = GetTileSpaceMatrix(size).Item1 * toCenter;

            batch.Begin(transformMatrix: mat);
        }

        protected void EndTile(UISpriteBatch batch)
        {
            batch.End();
            batch.Resume();
        }

        protected void DrawLine(UISpriteBatch batch, Vector3 from, Vector3 to, float lineWidth, Color color)
        {
            var px = TextureGenerator.GetPxWhite(batch.GraphicsDevice);

            float heightScale = -32f * TileScale;

            var fromScreen = Vector2.Transform(new Vector2(from.X, from.Y), TileMatrix) + new Vector2(0, from.Z * heightScale);
            var toScreen = Vector2.Transform(new Vector2(to.X, to.Y), TileMatrix) + new Vector2(0, to.Z * heightScale);

            var hSize = Size / 2;
            var fromOrigin = LocalPoint(fromScreen + hSize);
            var toOrigin = LocalPoint(toScreen + hSize);
            var dir = toOrigin - fromOrigin;
            var dist = dir.Length();

            float rotation = (float)Math.Atan2(dir.Y, dir.X);

            batch.Draw(px, fromOrigin - new Vector2(0, lineWidth / -2), null, color, rotation, new Vector2(0, 0.5f), new Vector2(dist, lineWidth), SpriteEffects.None, 0);
        }
    }
}
