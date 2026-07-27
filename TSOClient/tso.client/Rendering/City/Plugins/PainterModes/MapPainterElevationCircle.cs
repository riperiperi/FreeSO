using FSO.Client.UI.Model;
using FSO.Common;
using FSO.Common.Rendering.Framework.Model;
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
        public CityEditBase Command => Painter.Flatten ? Flatten.Command : BuildCommand();

        private Dictionary<Point, float> ElevationMod;
        private int ElevationFrames = 0;
        private bool MouseDown;
        private readonly MapPainterElevationFlat Flatten;
        private readonly MapPainterSpraypaint Spray;

        public MapPainterElevationCircle(MapPainterPlugin painter)
        {
            Painter = painter;
            Flatten = new MapPainterElevationFlat(painter);
            Spray = new MapPainterSpraypaint();
            Spray.NewSeed();
        }

        private static bool InBounds(Point loc)
        {
            return loc.X >= 0 && loc.Y >= 0 && loc.X < 512 && loc.Y < 512;
        }

        public void Draw(SpriteBatch sb)
        {
            if (Painter.Flatten)
            {
                Flatten.Draw(sb);
                return;
            }

            var ePos = new Point((int)Math.Round(LastPos.X), (int)Math.Round(LastPos.Y));

            var erasing = Painter.Erasing;
            var rough = Painter.RoughTerrain;
            float baseMul = erasing ? -1 : 1;

            IMapPainterMode.BrushFunc(Painter.BrushSize, (x, y, strength) =>
            {
                var loc = new Point(ePos.X + x, ePos.Y + y);

                if (InBounds(loc))
                {
                    var multiplier = baseMul * (Painter.Accelerate ? 2f : 1f);
                    var eOnScreen = City.Get2DFromTile(ePos.X + x, ePos.Y + y);

                    var color = erasing ? Color.Red : Color.White;

                    float alpha = 0.2f + strength * 0.55f;

                    if (rough)
                    {
                        strength = Spray.GetRoughEdge(loc.X * 512 + loc.Y, strength, Painter.BrushSize);
                    }

                    City.DrawSpike(loc.ToVector2(), strength * multiplier * 1.5f, sb, 196, color * alpha);

                    /*
                    City.DrawLine(TextureGenerator.GetPxWhite(sb.GraphicsDevice), eOnScreen, eOnScreen + new Vector2(0, -50) * strength * multiplier, sb, 3, 100);
                    */
                }
            });
        }

        public void TileHover(Vector2? tile)
        {
            if (Painter.Flatten)
            {
                Flatten.TileHover(tile);
                return;
            }

            if (MouseDown)
            {
                var frameMul = 60f / FSOEnvironment.RefreshRate;
                var wallPos = new Point((int)Math.Round(tile.Value.X), (int)Math.Round(tile.Value.Y));
                var size = Painter.BrushSize;
                var multiplier = Painter.BrushIntensity * (Painter.Accelerate ? 4f : 2f);
                var rough = Painter.RoughTerrain;

                IMapPainterMode.BrushFunc(size, (x, y, strength) =>
                {
                    if (strength > 0)
                    {
                        var loc = new Point(wallPos.X + x, wallPos.Y + y);

                        if (rough && InBounds(loc))
                        {
                            strength = Spray.GetRoughEdge(loc.X * 512 + loc.Y, strength, Painter.BrushSize);
                        }

                        if (ElevationMod.ContainsKey(loc)) ElevationMod[loc] += ((Painter.Erasing) ? -1 : 1) * strength * multiplier * frameMul / 5;
                        else ElevationMod[loc] = ((Painter.Erasing) ? -1 : 1) * strength * multiplier * frameMul / 5;
                    }
                });
            }

            if (tile != null) LastPos = tile.Value;
        }

        public void TileMouseDown(Vector2 tile)
        {
            HIT.HITVM.Get().PlaySoundEvent(UISounds.BuildDragToolDown);
            if (Painter.Flatten)
            {
                Flatten.TileMouseDown(tile);
                return;
            }

            Spray.NewSeed();

            ElevationMod = new Dictionary<Point, float>();
            ElevationFrames = 0;
            MouseDown = true;
        }

        public void TileMouseUp(Vector2? tile)
        {
            if (Painter.Flatten)
            {
                Flatten.TileMouseUp(tile);
                return;
            }

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
            if (Painter.Flatten)
            {
                Flatten.Update(state);
                return;
            }

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

            if (alt.Bitmap == null)
            {
                return null;
            }

            return alt;
        }
    }
}
