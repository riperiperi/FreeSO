using FSO.Client.UI.Model;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
using FSO.Common.Utils;
using FSO.Server.Protocol.Electron.Model.CityEditCommands;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace FSO.Client.Rendering.City.Plugins.PainterModes
{
    internal class MapPainterElevationFlat : IMapPainterMode
    {
        private readonly MapPainterPlugin Painter;
        private Terrain City => Painter.City;
        private Vector2 LastPos;
        public CityEditBase Command => BuildCommand();

        private Dictionary<Point, float> ElevationMod;
        private int ElevationFrames = 0;

        private bool MouseDown;

        public MapPainterElevationFlat(MapPainterPlugin painter)
        {
            Painter = painter;
        }

        public void Draw(SpriteBatch sb)
        {
            var ePos = new Point((int)Math.Round(LastPos.X), (int)Math.Round(LastPos.Y));

            var elevations = new List<byte>();
            IMapPainterMode.BrushFunc(Painter.BrushSize, (x, y, strength) =>
            {
                var index = ePos.X + x + (ePos.Y + y) * 512;
                if (index < 0 || index > City.MapData.ElevationData.Length) return;
                elevations.Add(City.MapData.ElevationData[index]);
            });

            if (elevations.Count == 0)
            {
                return;
            }

            var sorted = elevations.OrderBy(x => x).ToList();
            var elevation = sorted[sorted.Count / 2]; //median
            var intensity = Painter.BrushIntensity;
            var pxWhite = TextureGenerator.GetPxWhite(sb.GraphicsDevice);

            IMapPainterMode.BrushFunc(Painter.BrushSize, (x, y, strength) =>
            {
                if (strength > 0)
                {
                    var multiplier = (Painter.Accelerate) ? 2 : 1;
                    var index = ePos.X + x + (ePos.Y + y) * 512;
                    if (index < 0 || index > City.MapData.ElevationData.Length) return;
                    var elev = City.MapData.ElevationData[index];

                    var change = (elevation - elev) / 50f;
                    if (change > 0) change = Math.Max(0.02f, change);
                    else change = Math.Min(-0.02f, change);

                    var alpha = (change < 0 ? 0.2f : 0.75f) * Math.Min(1.1f, 0.2f + intensity * 0.6f * multiplier);
                    City.DrawSpike(new Vector2(ePos.X + x, ePos.Y + y), change * 5, sb, 192, Color.White * alpha);
                }
            });
        }

        public void TileHover(Vector2? tile)
        {
            if (MouseDown)
            {
                var frameMul = 60f / FSOEnvironment.RefreshRate;
                var wallPos = new Point((int)Math.Round(tile.Value.X), (int)Math.Round(tile.Value.Y));
                var size = Painter.BrushSize;

                var elevations = new List<byte>();
                IMapPainterMode.BrushFunc(size, (x, y, strength) =>
                {
                    var index = wallPos.X + x + (wallPos.Y + y) * 512;
                    if (index < 0 || index > City.MapData.ElevationData.Length) return;
                    elevations.Add(City.MapData.ElevationData[index]);
                });

                var sorted = elevations.OrderBy(x => x).ToList();
                var elevation = sorted[sorted.Count / 2]; //median

                var multiplier = Painter.BrushIntensity * (Painter.Accelerate ? 4f : 2f);

                IMapPainterMode.BrushFunc(size, (x, y, strength) =>
                {
                    if (strength > 0)
                    {
                        var index = wallPos.X + x + (wallPos.Y + y) * 512;
                        if (index < 0 || index > City.MapData.ElevationData.Length) return;
                        var elev = City.MapData.ElevationData[index];

                        var loc = new Point(wallPos.X + x, wallPos.Y + y);
                        var change = frameMul * (elevation - elev) / 50f * multiplier;
                        if (change > 0) change = Math.Max(0.02f, change);
                        else change = Math.Min(-0.02f, change);

                        if (ElevationMod.ContainsKey(loc)) ElevationMod[loc] += change;
                        else ElevationMod[loc] = change;
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
                HIT.HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolUp);
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

            alt.AutoTerrainType = Painter.AutoTerrain;
            alt.Bitmap = bitmap;
            alt.AltitudeDeltas = deltas;
            alt.Trim();

            return alt;
        }
    }
}
