using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.Rendering.City.Plugins.PainterModes
{
    internal class MapPainterElevationCircle : IMapPainterMode
    {
        private readonly MapPainterPlugin Painter;
        private Terrain City => Painter.City;
        private Vector2 LastPos;
        public CityEditBase Command => BuildCommand();

        private Dictionary<Point, float> ElevationMod;
        private int ElevationFrames = 0;
        private bool MouseDown;

        public MapPainterElevationCircle(MapPainterPlugin painter)
        {
            Painter = painter;
        }

        public void Draw(SpriteBatch sb)
        {
            var ePos = new Point((int)Math.Round(LastPos.X), (int)Math.Round(LastPos.Y));

            IMapPainterMode.BrushFunc(Painter.BrushSize, (x, y, strength) =>
            {
                //if (strength <= 0) return;
                var multiplier = (Painter.Accelerate) ? 2 : 1;
                var eOnScreen = City.Get2DFromTile(ePos.X + x, ePos.Y + y);
                City.DrawLine(TextureGenerator.GetPxWhite(sb.GraphicsDevice), eOnScreen, eOnScreen + new Vector2(0, -50) * strength * multiplier, sb, 3, 100);
            });
        }

        public void TileHover(Vector2? tile)
        {
            if (MouseDown)
            {
                var frameMul = 60f / FSOEnvironment.RefreshRate;
                var wallPos = new Point((int)Math.Round(tile.Value.X), (int)Math.Round(tile.Value.Y));
                var size = Painter.BrushSize;
                IMapPainterMode.BrushFunc(size, (x, y, strength) =>
                {
                    var multiplier = (Painter.Accelerate) ? 2 : 1;
                    if (strength > 0)
                    {
                        var loc = new Point(wallPos.X + x, wallPos.Y + y);
                        if (ElevationMod.ContainsKey(loc)) ElevationMod[loc] += ((Painter.Erasing) ? -1 : 1) * strength * multiplier * frameMul / 5;
                        else ElevationMod[loc] = ((Painter.Erasing) ? -1 : 1) * strength * multiplier * frameMul / 5;
                    }
                });
            }

            if (tile != null) LastPos = tile.Value;
        }

        public void TileMouseDown(Vector2 tile)
        {
            ElevationMod = new Dictionary<Point, float>();
            ElevationFrames = 0;
            MouseDown = true;
        }

        public void TileMouseUp(Vector2? tile)
        {
            if (ElevationMod != null)
            {
                Painter.Commit(ElevationMod.Count != 0);
            }

            ElevationMod = null;
            MouseDown = false;
        }

        public void Update(UpdateState state)
        {
            var frameMul = FSOEnvironment.RefreshRate / 60f;
            if (ElevationMod != null && ElevationFrames-- <= 0)
            {
                Painter.UpdateTemp();
                ElevationFrames = (int)(5 * frameMul);
            }
        }

        private CityEditAltitude BuildCommand()
        {
            if (ElevationMod == null)
            {
                return null;
            }

            var alt = new CityEditAltitude();

            int width = City.MapData.Width;
            int height = City.MapData.Height;

            var bitmap = new CityEditBitmap(width, height);
            short[] deltas = new short[width * height];

            foreach (var mod in ElevationMod)
            {
                if (mod.Key.X < 0 || mod.Key.Y < 0 || mod.Key.X >= width || mod.Key.Y >= height) continue;
                var index = mod.Key.X + mod.Key.Y * width;
                bitmap.Set(mod.Key.X, mod.Key.Y);
                deltas[index] = (short)Math.Clamp(Math.Round(mod.Value), short.MinValue, short.MaxValue);
            }

            alt.Bitmap = bitmap;
            alt.AltitudeDeltas = deltas;
            alt.Trim();

            return alt;
        }
    }
}
